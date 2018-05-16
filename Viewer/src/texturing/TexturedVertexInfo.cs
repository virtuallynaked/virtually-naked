using SharpDX;

public struct TexturedVertexInfo {
	public Vector2 TexCoord { get; }
	public Vector2 TangentUCoeffs { get; }

	public TexturedVertexInfo(Vector2 texCoord, Vector2 tangentUCoeffs) {
		TexCoord = texCoord;
		TangentUCoeffs = tangentUCoeffs;
	}
}
