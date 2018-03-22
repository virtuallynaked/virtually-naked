using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using Buffer = SharpDX.Direct3D11.Buffer;

public class FigureSurface : IDisposable {
	private static int[] Triangularize(List<Quad> faces) {
		int[] faceTriIndices = new int[faces.Count * 6];
		for (int i = 0; i < faces.Count; ++i) {
			Quad face = faces[i];

			faceTriIndices[i * 6 + 0] = face.Index0;
			faceTriIndices[i * 6 + 1] = face.Index1;
			faceTriIndices[i * 6 + 2] = face.Index2;

			faceTriIndices[i * 6 + 3] = face.Index2;
			faceTriIndices[i * 6 + 4] = face.Index3;
			faceTriIndices[i * 6 + 5] = face.Index0;
		}
		return faceTriIndices;
	}

	public static FigureSurface[] MakeSurfaces(Device device, int surfaceCount, Quad[] allFaces, int[] surfaceMap) {
		List<List<Quad>> surfaceFaces = Enumerable.Range(0, surfaceCount).Select(idx => new List<Quad>()).ToList();

		for (int faceIdx = 0; faceIdx < allFaces.Length; ++faceIdx) {
			int surfaceIdx = surfaceMap[faceIdx];
			surfaceFaces[surfaceIdx].Add(allFaces[faceIdx]);
		}

		return surfaceFaces.Select(faces => MakeSurface(device, faces)).ToArray();
	}

	public static FigureSurface MakeSurface(Device device, List<Quad> faces) {
		int[] texturedIndices = Triangularize(faces);
		Buffer indexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, texturedIndices);
		return new FigureSurface(texturedIndices.Length, indexBuffer);
	}

	private readonly int indexCount;
	private readonly Buffer indexBuffer;

	public FigureSurface(int indexCount, Buffer indexBuffer) {
		this.indexCount = indexCount;
		this.indexBuffer = indexBuffer;
	}

	public void Dispose() {
		indexBuffer.Dispose();
	}

	public void Draw(DeviceContext context) {
		context.InputAssembler.SetIndexBuffer(indexBuffer, SharpDX.DXGI.Format.R32_UInt, 0);
		context.DrawIndexed(indexCount, 0, 0);
	}
}
