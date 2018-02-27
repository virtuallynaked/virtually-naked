public struct BoneAttributes {
	public bool IsIkable { get; }
	public float Mass { get; }

	public BoneAttributes(bool isIkable, float mass) {
		IsIkable = isIkable;
		Mass = mass;
	}

	public override string ToString() {
		return string.Format("{{ ikIkable = {0}, mass = {1} }}", IsIkable, Mass);
	}
}
