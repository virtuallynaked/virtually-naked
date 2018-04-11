using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;

public struct SharedTexture : IDisposable {
	private TextureCache.Entry cacheEntry;
	
	public SharedTexture(TextureCache.Entry cacheEntry) {
		cacheEntry.AddRef();
		this.cacheEntry = cacheEntry;
	}
	
	public void Dispose() {
		cacheEntry.Release();
	}

	public static implicit operator ShaderResourceView(SharedTexture sharedTexture) {
		return sharedTexture.cacheEntry.Resource;
	}
}

public class TextureCache {
	public class Entry {
		private readonly TextureCache loader;
		private readonly IArchiveFile key;
		private readonly ShaderResourceView resource;
		private int referenceCount;
		
		public Entry(TextureCache loader, IArchiveFile key, ShaderResourceView resource) {
			this.loader = loader;
			this.key = key;
			this.resource = resource;
			referenceCount = 0;
		}

		public ShaderResourceView Resource => resource;

		public void AddRef() {
			referenceCount += 1;
		}

		public void Release() {
			lock (loader) {
				referenceCount -= 1;
				if (referenceCount == 0) {
					resource.Dispose();
					loader.RemoveEntry(key);
				}
			}
		}
	}
	
	private readonly Device device;
	private readonly Dictionary<IArchiveFile, Entry> dict = new Dictionary<IArchiveFile, Entry>();
	
	public TextureCache(Device device) {
		this.device = device;
	}

	public SharedTexture Get(IArchiveFile file) {
		lock (this) {
			if (!dict.TryGetValue(file, out var cacheEntry)) {
				cacheEntry = AddEntry(file);
			}
			return new SharedTexture(cacheEntry);
		}
	}

	private ShaderResourceView Load(IArchiveFile file) {
		using (var dataView = file.OpenDataView()) {
			DdsLoader.CreateDDSTextureFromMemory(device, dataView.DataPointer, out var texture, out var textureView);
			texture.Dispose();
			return textureView;
		}
	}

	private Entry AddEntry(IArchiveFile key) {
		//Console.WriteLine($"loading {key.Name}");
		ShaderResourceView resource = Load(key);
		var entry = new Entry(this, key, resource);
		dict.Add(key, entry);
		return entry;
	}

	private void RemoveEntry(IArchiveFile key) {
		//Console.WriteLine($"unloading {key.Name}");
		dict.Remove(key);
	}
}
