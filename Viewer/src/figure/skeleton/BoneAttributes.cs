public struct BoneAttributes {
	public bool IsIkable { get; }
	public MassMoment MassMoment { get; }
	public float MassIncludingDescendants { get; }

	public BoneAttributes(bool isIkable, MassMoment massMoment, float massIncludingDescendants) {
		IsIkable = isIkable;
		MassMoment = massMoment;
		MassIncludingDescendants = massIncludingDescendants;
	}

	public override string ToString() {
		return string.Format("{{ IkIkable = {0}, MassMoment = {1}, MassIncludingDescendants = {2} }}", IsIkable, MassMoment, MassIncludingDescendants);
	}
}
