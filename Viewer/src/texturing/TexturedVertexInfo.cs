using SharpDX;

public struct TexturedVertexInfo {
	public int SpacialInfoIdx { get; }
	public Vector2 TexCoord { get; }
	public Vector2 TexCoordDu { get; }
	public Vector2 TexCoordDv { get; }

	public TexturedVertexInfo(int positionInfoIdx, Vector2 texCoord, Vector2 texCoordDu, Vector2 texCoordDv) {
		SpacialInfoIdx = positionInfoIdx;
		TexCoord = texCoord;
		TexCoordDu = texCoordDu;
		TexCoordDv = texCoordDv;
	}
}
