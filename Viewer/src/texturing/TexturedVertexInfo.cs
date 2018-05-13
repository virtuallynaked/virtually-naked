using SharpDX;

public struct TexturedVertexInfo {
	public int SpacialInfoIdx { get; }
	public Vector2 TexCoord { get; }
	public Vector2 TangentUCoeffs { get; }

	public TexturedVertexInfo(int positionInfoIdx, Vector2 texCoord, Vector2 tangentUCoeffs) {
		SpacialInfoIdx = positionInfoIdx;
		TexCoord = texCoord;
		TangentUCoeffs = tangentUCoeffs;
	}
}
