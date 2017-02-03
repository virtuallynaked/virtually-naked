using SharpDX;
using System;
using System.Collections.Immutable;
using System.Linq;

public class HdMorph {
	public struct VertexEdit {
		public uint PackedPath { get; }
		public Vector3 Delta { get; }

		public VertexEdit(uint packedPath, Vector3 delta) {
			PackedPath = packedPath;
			Delta = delta;
		}

		public int PathLength => (int) (PackedPath >> 28);

		public static uint PackPath(params int[] pathElements) {
			uint packedPath = (uint) pathElements.Length << 28;
			for (int idx = 0; idx < pathElements.Length; ++idx) {
				packedPath |= (uint) pathElements[idx] << (22 - idx * 2);
			}
			return packedPath;
		}

		public int GetPathElement(int idx) {
			return (int) (PackedPath >> (22 - idx * 2)) & 0x3;
		}

		public int[] Path {
			get {
				var path = new int[PathLength];
				for (int i = 0; i < PathLength; ++i) {
					path[i] = GetPathElement(i);
				}
				return path;
			}
		}
	}

	public class FaceEdit {
		public int ControlFaceIdx { get; }
		public ImmutableArray<VertexEdit> VertexEdits { get; }

		public FaceEdit(int controlFaceIdx, ImmutableArray<VertexEdit> vertexEdits) {
			ControlFaceIdx = controlFaceIdx;
			VertexEdits = vertexEdits;
		}
	}

	public class Level {
		public int ControlFaceCount { get; } //this is the total number of faces in the figure, not size of the faces collection
		public int LevelIdx { get; }
		public ImmutableArray<FaceEdit> FaceEdits { get; }

		public Level(int controlFaceCount, int levelIdx, ImmutableArray<FaceEdit> faceEdits) {
			ControlFaceCount = controlFaceCount;
			LevelIdx = levelIdx;
			FaceEdits = faceEdits;
		}
	}

	public ImmutableArray<Level> Levels { get; }

	public HdMorph(ImmutableArray<Level> levels) {
		Levels = levels;

		for (int i = 0; i < Levels.Length; ++i) {
			if (levels[i].LevelIdx != i + 1) {
				throw new InvalidOperationException("unexpected level-idx");
			}
		}
	}

	public int MaxLevel => Levels.Last().LevelIdx;

	public void Validate(int figureControlFaceCount) {
		foreach (var level in Levels) {
			if (level.ControlFaceCount != figureControlFaceCount) {
				throw new InvalidOperationException("wrong face count");
			}
		}
	}
}
