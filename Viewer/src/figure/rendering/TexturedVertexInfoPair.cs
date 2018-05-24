using System;

public struct TexturedVertexInfoPair {
	public TexturedVertexInfo Primary { get; }
	public TexturedVertexInfo Secondary { get; }

	public TexturedVertexInfoPair(TexturedVertexInfo primary, TexturedVertexInfo secondary) {
		Primary = primary;
		Secondary = secondary;
	}

	public static TexturedVertexInfoPair[] Interleave(TexturedVertexInfo[] primaries, TexturedVertexInfo[] secondaries) {
		int count = primaries.Length;
		if (secondaries != null && count != secondaries.Length) {
			throw new ArgumentException("count mismatch");
		}

		TexturedVertexInfoPair[] pairs = new TexturedVertexInfoPair[count];
		for (int i = 0; i < count; ++i) {
			pairs[i] = new TexturedVertexInfoPair(primaries[i], secondaries != null ? secondaries[i] : default(TexturedVertexInfo));
		}
		return pairs;
	}
}
