public class MaterialSetOption {
	public static MaterialSetOption Load(IArchiveDirectory shapeDirectory) {
		return new MaterialSetOption(shapeDirectory.Name, shapeDirectory);
	}
	
	public string Label { get; }
	public IArchiveDirectory Directory { get; }

	public MaterialSetOption(string label, IArchiveDirectory directory) {
		Label = label;
		Directory = directory;
	}
}
