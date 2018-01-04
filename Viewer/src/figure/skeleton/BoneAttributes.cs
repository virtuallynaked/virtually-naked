public class BoneAttributes {
	public float Mass { get; }

	public BoneAttributes(float mass) {
		Mass = mass;
	}

	public override string ToString() {
		return string.Format("{{ mass = {0} }}", Mass);
	}
}
