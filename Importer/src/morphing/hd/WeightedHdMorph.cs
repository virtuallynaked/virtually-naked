public struct WeightedHdMorph {
	public HdMorph Morph { get; }
	public float Weight { get; }

	public WeightedHdMorph(HdMorph morph, float weight) {
		Morph = morph;
		Weight = weight;
	}
}
