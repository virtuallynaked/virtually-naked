using System;

public struct ShaderAndBytecode<T> {
	public T Shader { get; }
	public byte[] Bytecode { get; }

	public ShaderAndBytecode(T shader, byte[] bytecode) {
		Shader = shader;
		Bytecode = bytecode;
	}

	public static implicit operator T(ShaderAndBytecode<T> shaderAndBytecode) {
		return shaderAndBytecode.Shader;
	}
}

