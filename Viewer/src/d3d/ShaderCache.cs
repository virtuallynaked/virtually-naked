using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

public partial class ShaderCache : IDisposable {
	public const string VertexShaderExtension = ".vs.cso";
	public const string PixelShaderExtension = ".ps.cso";
	public const string ComputeShaderExtension = ".cs.cso";

	private Device device;

	private struct ShaderKey {
		private readonly Type type;
		private readonly string name;

		public ShaderKey(Type type, string name) {
			this.type = type;
			this.name = name;
		}

		public bool Equals(ShaderKey other) {
			return this.type == other.type && this.name == other.name;
		}

		public override int GetHashCode() {
			return type.GetHashCode() ^ name.GetHashCode();
		}

		public override bool Equals(object otherObj) {
			switch (otherObj) {
				case ShaderKey otherKey:
					return type == otherKey.type && name == otherKey.name;
				default:
					return false;
			}
		}
	}

	private Dictionary<ShaderKey, ShaderAndBytecode<VertexShader>> vertexShaders = new Dictionary<ShaderKey, ShaderAndBytecode<VertexShader>>();
	private Dictionary<ShaderKey, ShaderAndBytecode<PixelShader>> pixelShaders = new Dictionary<ShaderKey, ShaderAndBytecode<PixelShader>>();
	private Dictionary<ShaderKey, ShaderAndBytecode<ComputeShader>> computeShaders = new Dictionary<ShaderKey, ShaderAndBytecode<ComputeShader>>();

	public ShaderCache(Device device) {
		this.device = device;
	}
	
	public void Dispose() {
		foreach (var value in vertexShaders.Values) {
			value.Shader.Dispose();
		}
		foreach (var value in pixelShaders.Values) {
			value.Shader.Dispose();
		}
		foreach (var value in computeShaders.Values) {
			value.Shader.Dispose();
		}
	}

	public static byte[] LoadBytesFromResource<T>(string resourceName) {
		Type type = typeof(T);
		Assembly assembly = type.Assembly;
		using (Stream stream = assembly.GetManifestResourceStream(resourceName)) {
			if (stream == null) {
				throw new ArgumentException("missing resource: " + resourceName);
			}
			using (var bytecode = ShaderBytecode.FromStream(stream)) {
				return bytecode.Data;
			}
		}
	}

	private static V GetOrMake<K,V>(Dictionary<K,V> dict, K key, Func<V> supplier) {
		if (dict.TryGetValue(key, out V value)) {
			return value;
		}
		value = supplier();
		dict.Add(key, value);
		return value;
	}
		
	public ShaderAndBytecode<ComputeShader> GetComputeShader<T>(string shaderName) {
		var key = new ShaderKey(typeof(T), shaderName);
		return GetOrMake(computeShaders, key, () => {
			var bytecode = LoadBytesFromResource<T>(shaderName + ComputeShaderExtension);
			var shader = new ComputeShader(device, bytecode);
			return new ShaderAndBytecode<ComputeShader>(shader, bytecode);
		});
	}

	public ShaderAndBytecode<PixelShader> GetPixelShader<T>(string shaderName) {
		var key = new ShaderKey(typeof(T), shaderName);
		return GetOrMake(pixelShaders, key, () => {
			var bytecode = LoadBytesFromResource<T>(shaderName + PixelShaderExtension);
			var shader = new PixelShader(device, bytecode);
			return new ShaderAndBytecode<PixelShader>(shader, bytecode);
		});
	}

	public ShaderAndBytecode<VertexShader> GetVertexShader<T>(string shaderName) {
		var key = new ShaderKey(typeof(T), shaderName);
		return GetOrMake(vertexShaders, key, () => {
			var bytecode = LoadBytesFromResource<T>(shaderName + VertexShaderExtension);
			var shader = new VertexShader(device, bytecode);
			return new ShaderAndBytecode<VertexShader>(shader, bytecode);
		});
	}
}
