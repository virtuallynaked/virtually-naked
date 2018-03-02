public struct BoneAttributes {
	public bool IsIkable { get; }
	public float Mass { get; }
	public float MassIncludingDescendants { get; }

	public BoneAttributes(bool isIkable, float mass, float massIncludingDescendants) {
		IsIkable = isIkable;
		Mass = mass;
		MassIncludingDescendants = massIncludingDescendants;
	}

	public override string ToString() {
		return string.Format("{{ IkIkable = {0}, Mass = {1}, MassIncludingDescendants = {2} }}", IsIkable, Mass, MassIncludingDescendants);
	}
}
