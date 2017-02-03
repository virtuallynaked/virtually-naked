using SharpDX;
using System;
using System.Collections.Immutable;
using System.IO;

public static class HdMorphSerialization {
	private const int LongPathLevelThreshold = 4;

	private static HdMorph.VertexEdit ReadVertexEdit(BinaryReader reader, int levelIdx) {
		float x = reader.ReadSingle();

		ushort packedPathLow;
		ushort packedPathHigh;
		if (levelIdx < LongPathLevelThreshold) {
			packedPathLow = 0;
			packedPathHigh = reader.ReadUInt16();
		} else {
			packedPathLow = reader.ReadUInt16();
			packedPathHigh = reader.ReadUInt16();
		}
		uint packedPath = ((uint) packedPathHigh << 16) | (uint) packedPathLow;

		float y = reader.ReadSingle();
		float z = reader.ReadSingle();

		var delta = new Vector3(x, y, z);
		var vertexEdit = new HdMorph.VertexEdit(packedPath, delta);

		if (vertexEdit.PathLength != levelIdx + 1) {
			throw new InvalidOperationException("path length mismatch");
		}

		return vertexEdit;
	}

	private static void WriterVertexEdit(BinaryWriter writer, int levelIdx, HdMorph.VertexEdit vertexEdit) {
		writer.Write(vertexEdit.Delta.X);

		ushort packedPathLow = (ushort) (vertexEdit.PackedPath & 0xffff);
		ushort packedPathHigh = (ushort) (vertexEdit.PackedPath >> 16);
		if (levelIdx < LongPathLevelThreshold) {
			writer.Write(packedPathHigh);
		} else {
			writer.Write(packedPathLow);
			writer.Write(packedPathHigh);
		}

		writer.Write(vertexEdit.Delta.Y);
		writer.Write(vertexEdit.Delta.Z);
	}

	private static HdMorph.FaceEdit ReadFaceEdit(BinaryReader reader, int levelIdx) {
		int controlFaceIdx = reader.ReadInt32();
		int vertexEditCount = reader.ReadInt32();
		
		var vertexEditsBuilder = ImmutableArray.CreateBuilder<HdMorph.VertexEdit>(vertexEditCount);
		for (int i = 0; i < vertexEditCount; ++i) {
			vertexEditsBuilder.Add(ReadVertexEdit(reader, levelIdx));
		}
		
		return new HdMorph.FaceEdit(controlFaceIdx, vertexEditsBuilder.ToImmutable());
	}

	private static void WriteFaceEdit(BinaryWriter writer, int levelIdx, HdMorph.FaceEdit faceEdit) {
		writer.Write(faceEdit.ControlFaceIdx);
		writer.Write(faceEdit.VertexEdits.Length);

		foreach (var vertexEdit in faceEdit.VertexEdits) {
			WriterVertexEdit(writer, levelIdx, vertexEdit);
		}
	}

	private static HdMorph.Level ReadLevel(BinaryReader reader) {
		int controlFaceCount = reader.ReadInt32();
		int levelIdx = reader.ReadInt32();
		
		int vertexEditCount = reader.ReadInt32();
		int sizeInBytes = reader.ReadInt32();
		
		long startPosition = reader.BaseStream.Position;

		var faceEditsBuilder = ImmutableArray.CreateBuilder<HdMorph.FaceEdit>(controlFaceCount);
		int runningCount = 0;
		while (runningCount < vertexEditCount) {
			HdMorph.FaceEdit face = ReadFaceEdit(reader, levelIdx);
			runningCount += face.VertexEdits.Length;
			faceEditsBuilder.Add(face);
		}

		if (runningCount != vertexEditCount) {
			throw new InvalidOperationException("record count mismatch");
		}
		
		long levelEndPosition = reader.BaseStream.Position;
		if (sizeInBytes != levelEndPosition - startPosition) {
			throw new InvalidOperationException("level size mismatch");
		}

		return new HdMorph.Level(controlFaceCount, levelIdx, faceEditsBuilder.ToImmutable());
	}
	
	private const int FaceEditHeaderSizeInBytes = sizeof(int) + sizeof(int);
	private const int ShortVertexEditSizeInBytes = sizeof(ushort) + 3 * sizeof(float);
	private const int LongVertexEditSizeInBytes = 2 * sizeof(ushort) + 3 * sizeof(float);

	private static void WriteLevel(BinaryWriter writer, HdMorph.Level level) {
		writer.Write(level.ControlFaceCount);
		writer.Write(level.LevelIdx);

		int vertexEditCount = 0;
		int sizeInBytes = 0;
		foreach (var faceEdit in level.FaceEdits) {
			sizeInBytes += FaceEditHeaderSizeInBytes;
			foreach (var vertexEdit in faceEdit.VertexEdits) {
				vertexEditCount += 1;
				sizeInBytes += level.LevelIdx < LongPathLevelThreshold ? ShortVertexEditSizeInBytes : LongVertexEditSizeInBytes;
			}
		}

		writer.Write(vertexEditCount);
		writer.Write(sizeInBytes);

		foreach (var faceEdit in level.FaceEdits) {
			WriteFaceEdit(writer, level.LevelIdx, faceEdit);
		}
	}

	private const uint Cookie = 0xd0d0d0d0;
	private const uint Unknown1 = 0x3f800000;

	public static HdMorph ReadHdMorph(BinaryReader reader) {
		uint cookie = reader.ReadUInt32();
		int levelCount = reader.ReadInt32();
		int unknown1 = reader.ReadInt32();
		int levelCountAgain = reader.ReadInt32();

		if (cookie != Cookie) {
			throw new InvalidOperationException("wrong cookie");
		}
		
		if (unknown1 != Unknown1) {
			throw new InvalidOperationException("wrong unknown1");
		}

		if (levelCountAgain != levelCount) {
			throw new InvalidOperationException("level count mismatch");
		}
		
		var levelsBuilder = ImmutableArray.CreateBuilder<HdMorph.Level>(levelCount);
		for (int i = 0; i < levelCount; ++i) {
			levelsBuilder.Add(ReadLevel(reader));
		}
		
		return new HdMorph(levelsBuilder.ToImmutable());
	}

	public static void WriteHdMorph(BinaryWriter writer, HdMorph morph) {
		writer.Write(Cookie);
		writer.Write(morph.Levels.Length);
		writer.Write(Unknown1);
		writer.Write(morph.Levels.Length);

		foreach (var level in morph.Levels) {
			WriteLevel(writer, level);
		}
	}

	public static HdMorph LoadHdMorph(FileInfo file) {
		using (var stream = file.OpenRead())
		using (var reader = new BinaryReader(stream)) {
			var hdMorph = HdMorphSerialization.ReadHdMorph(reader);

			if (reader.BaseStream.Position != reader.BaseStream.Length) {
				throw new InvalidOperationException("not at end of file");
			}

			return hdMorph;
		}
	}

	public static void SaveHdMorph(FileInfo file, HdMorph hdMorph) {
		using (var stream = file.OpenWrite())
		using (var writer = new BinaryWriter(stream)) {
			WriteHdMorph(writer, hdMorph);
		}
	}
}
