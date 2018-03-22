public class FigureUris {
	public string BasePath {get; }
	public string DocumentName {get; }
	public string GeometryId { get; }
	public string RootNodeId { get; }
	public string SkinBindingId {get; }

	public string DocumentUri => BasePath + DocumentName;
	public string GeometryUri => DocumentUri + "#" + GeometryId;
	public string RootNodeUri => DocumentUri + "#" + RootNodeId;
	public string SkinBindingUri => DocumentUri + "#" + SkinBindingId;
	
	public string MorphsBasePath => BasePath + "Morphs/";

	public string UvSetsBasePath => BasePath + "UV Sets/";

	public FigureUris(string basePath, string documentName, string geometryId, string rootNodeId, string skinBindingId) {
		BasePath = basePath;
		DocumentName = documentName;
		GeometryId = geometryId;
		RootNodeId = rootNodeId;
		SkinBindingId = skinBindingId;
	}
}
