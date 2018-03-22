using SharpDX;

public class UvSet {
	public string Name { get; }
	public Vector2[] Uvs { get; }
	public Quad[] Faces { get; }

	public UvSet(string name, Vector2[] uvs, Quad[] faces) {
		Name = name;
		Uvs = uvs;
		Faces = faces;
	}
}
