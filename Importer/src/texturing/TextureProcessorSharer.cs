using SharpDX.Direct3D11;
using System.Collections.Generic;

public class TextureProcessorSharer {
	private readonly Device device;
	private readonly ShaderCache shaderCache;
	private readonly bool compress;
	private readonly Dictionary<string, TextureProcessor> processors = new Dictionary<string, TextureProcessor>();

	public TextureProcessorSharer(Device device, ShaderCache shaderCache, bool compress) {
		this.device = device;
		this.shaderCache = shaderCache;
		this.compress = compress;
	}

	public TextureProcessor GetSharedProcessor(string shareName) {
		if (!processors.TryGetValue(shareName, out var processor)) {
			var destinationFolder = CommonPaths.WorkDir.Subdirectory("textures").Subdirectory(shareName);
			processor = new TextureProcessor(device, shaderCache, destinationFolder, compress);
			processors.Add(shareName, processor);
		}
		return processor;
	}

	public void Finish() {
		foreach (var processor in processors.Values) {
			processor.ImportAll();
		}
	}
}
