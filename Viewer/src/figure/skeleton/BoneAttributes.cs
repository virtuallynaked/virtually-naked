using SharpDX;

public struct BoneAttributes {
	public bool IsIkable { get; }
	public float Mass { get; }
	public Vector3 CenterOfMass { get; }
	public float MassIncludingDescendants { get; }

	public BoneAttributes(bool isIkable, float mass, Vector3 centerOfMass, float massIncludingDescendants) {
		IsIkable = isIkable;
		Mass = mass;
		CenterOfMass = centerOfMass;
		MassIncludingDescendants = massIncludingDescendants;
	}

	public override string ToString() {
		return string.Format("{{ IkIkable = {0}, Mass = {1}, CenterOfMass = {2}, MassIncludingDescendants = {3} }}", IsIkable, Mass, CenterOfMass, MassIncludingDescendants);
	}
}
