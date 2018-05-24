using SharpDX.Direct3D11;
using System;
using System.Linq;

public class ShapeNormalsLoader {
	private readonly IArchiveDirectory dataDir;
	private readonly Device device;
	private readonly TextureCache textureCache;

	public ShapeNormalsLoader(IArchiveDirectory dataDir, Device device, TextureCache textureCache) {
		this.device = device;
		this.dataDir = dataDir;
		this.textureCache = textureCache;
	}
	
	public ShapeNormals Load(IArchiveDirectory figureDir, Shape shape) {
		var file = shape.Directory?.File("shape-normals.dat");
		if (file == null) {
			return null;
		}

		var recipe = Persistance.Load<ShapeNormalsRecipe>(file);

		var uvSetName = recipe.UvSetName;
		IArchiveDirectory uvSetsDirectory = figureDir.Subdirectory("uv-sets");
		IArchiveDirectory uvSetDirectory = uvSetsDirectory.Subdirectory(uvSetName);
		var texturedVertexInfos = uvSetDirectory.File("textured-vertex-infos.array").ReadArray<TexturedVertexInfo>();

		var texturesDirectory = dataDir.Subdirectory("textures");
		var textureLoader = new TextureLoader(device, textureCache, texturesDirectory);
		var normalMapsBySurface = recipe.TextureNamesBySurface
			.Select(name => name != ShapeNormalsRecipe.DefaultTextureName ? name : null)
			.Select(name => textureLoader.Load(name, TextureLoader.DefaultMode.Bump))
			.ToArray();

		return new ShapeNormals(textureLoader, texturedVertexInfos, normalMapsBySurface);
	}
}

public class ShapeNormalsRecipe {
	public const string DefaultTextureName = "Default";

	public string UvSetName { get; }
	public string[] TextureNamesBySurface { get; }

	public ShapeNormalsRecipe(string uvSetName, string[] textureNamesBySurface) {
		UvSetName = uvSetName;
		TextureNamesBySurface = textureNamesBySurface;
	}
}

public class ShapeNormals : IDisposable {
	private readonly TextureLoader textureLoader;
	public TexturedVertexInfo[] TexturedVertexInfos { get; }
	public ShaderResourceView[] NormalsMapsBySurface { get; }

	public ShapeNormals(TextureLoader textureLoader, TexturedVertexInfo[] texturedVertexInfos, ShaderResourceView[] normalsMapsBySurface) {
		this.textureLoader = textureLoader;
		TexturedVertexInfos = texturedVertexInfos;
		NormalsMapsBySurface = normalsMapsBySurface;
	}

	public void Dispose() {
		textureLoader.Dispose();
	}
}
