public class SurfaceProperties {
	public int SubdivisionLevel { get; }
	public int[] RenderOrder { get; }
	public float[] Opacities { get; }
	public bool PrecomputeScattering { get; }
	public string ShareTextures { get; }

	public SurfaceProperties(int subdivisionLevel, int[] renderOrder, float[] opacities, bool precomputeScattering, string shareTextures) {
		SubdivisionLevel = subdivisionLevel;
		RenderOrder = renderOrder ?? new int[0];
		Opacities = opacities ?? new float[0];
		PrecomputeScattering = precomputeScattering;
		ShareTextures = shareTextures;
	}
}
