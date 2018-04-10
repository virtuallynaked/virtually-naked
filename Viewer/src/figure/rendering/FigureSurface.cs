using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using Buffer = SharpDX.Direct3D11.Buffer;

public struct Uint32IndexBuffer : IDisposable {
	private static int[] Triangularize(List<Quad> faces) {
		List<int> faceTriIndices = new List<int>(faces.Count * 6);
		for (int i = 0; i < faces.Count; ++i) {
			Quad face = faces[i];

			faceTriIndices.Add(face.Index0);
			faceTriIndices.Add(face.Index1);
			faceTriIndices.Add(face.Index2);

			if (!face.IsDegeneratedIntoTriangle) {
				faceTriIndices.Add(face.Index2);
				faceTriIndices.Add(face.Index3);
				faceTriIndices.Add(face.Index0);
			}
		}
		return faceTriIndices.ToArray();
	}

	public static Uint32IndexBuffer Make(Device device, List<Quad> faces) {
		int[] indices = Triangularize(faces);
		Buffer indexBuffer = indices.Length == 0 ? null : Buffer.Create(device, BindFlags.IndexBuffer, indices, 0, ResourceUsage.Immutable);
		return new Uint32IndexBuffer(indices.Length, indexBuffer);
	}

	private readonly int count;
	private readonly Buffer buffer;

	public Uint32IndexBuffer(int count, Buffer buffer) {
		if (buffer == null && count != 0) {
			throw new ArgumentNullException("buffer can only be null if count is 0");
		}

		this.count = count;
		this.buffer = buffer;
	}

	public void Dispose() {
		buffer?.Dispose();
	}

	public void Draw(DeviceContext context) {
		if (buffer == null) {
			return;
		}
		context.InputAssembler.SetIndexBuffer(buffer, SharpDX.DXGI.Format.R32_UInt, 0);
		context.DrawIndexed(count, 0, 0);
	}
}

public class FigureSurface : IDisposable {
	public static FigureSurface[] MakeSurfaces(Device device, int surfaceCount, Quad[] allFaces, int[] controlFaceMap, int[] surfaceMap, float[] faceTransparencies) {
		List<List<Quad>> opaqueFacesBySurface = Enumerable.Range(0, surfaceCount).Select(idx => new List<Quad>()).ToList();
		List<List<Quad>> transparentFacesBySurface = Enumerable.Range(0, surfaceCount).Select(idx => new List<Quad>()).ToList();

		for (int faceIdx = 0; faceIdx < allFaces.Length; ++faceIdx) {
			int controlFaceIdx = controlFaceMap[faceIdx];
			int surfaceIdx = surfaceMap[controlFaceIdx];
			float transparency = faceTransparencies[controlFaceIdx];
			bool isOpaque = MathUtil.IsZero(transparency);
			
			if (isOpaque) {
				opaqueFacesBySurface[surfaceIdx].Add(allFaces[faceIdx]);
			} else {
				transparentFacesBySurface[surfaceIdx].Add(allFaces[faceIdx]);
			}
		}

		var surfaces = Enumerable.Range(0, surfaceCount)
			.Select(surfaceIdx => {
				var opaqueFaces = opaqueFacesBySurface[surfaceIdx];
				var transparentFaces = transparentFacesBySurface[surfaceIdx];
				return FigureSurface.MakeSurface(device, opaqueFaces, transparentFaces);
			})
			.ToArray();
		return surfaces;
	}

	public static FigureSurface MakeSurface(Device device, List<Quad> opaqueFaces, List<Quad> transparentFaces) {
		var opaqueIndexBuffer = Uint32IndexBuffer.Make(device, opaqueFaces);
		var transparentIndexBuffer = Uint32IndexBuffer.Make(device, transparentFaces);
		return new FigureSurface(opaqueIndexBuffer, transparentIndexBuffer);
	}

	private readonly Uint32IndexBuffer opaqueIndexBuffer;
	private readonly Uint32IndexBuffer transparentIndexBuffer;

	public FigureSurface(Uint32IndexBuffer opaqueIndexBuffer, Uint32IndexBuffer transparentIndexBuffer) {
		this.opaqueIndexBuffer = opaqueIndexBuffer;
		this.transparentIndexBuffer = transparentIndexBuffer;
	}

	public void Dispose() {
		opaqueIndexBuffer.Dispose();
		transparentIndexBuffer.Dispose();
	}

	public void DrawOpaque(DeviceContext context) {
		opaqueIndexBuffer.Draw(context);
	}

	public void DrawTransparent(DeviceContext context) {
		transparentIndexBuffer.Draw(context);
	}
}
