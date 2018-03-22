using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX.D3DCompiler;
using System.Reflection;
using System.Runtime.InteropServices;

public static class MaterialTestCommon {
	public static void TestConstantsStructMatchesShader<Material, ConstantsStruct>(string shaderName, string bufferName) {
		var fields = typeof(ConstantsStruct).GetFields();
		var bytecode = ShaderCache.LoadBytesFromResource<Material>(shaderName + ShaderCache.PixelShaderExtension);

		using (var reflection = new ShaderReflection(bytecode))
		using (var buffer = reflection.GetConstantBuffer(bufferName)) {
			int variableCount = buffer.Description.VariableCount;
			Assert.AreEqual(buffer.Description.Size, Marshal.SizeOf<ConstantsStruct>(), "struct size mismatch");
			Assert.AreEqual(variableCount, fields.Length, "field count mismatch");

			for (int i = 0; i < variableCount; ++i) {
				FieldInfo field = fields[i];

				using (var variable = buffer.GetVariable(i)) {
					Assert.AreEqual(
						variable.Description.Name.ToLowerInvariant(),
						field.Name.ToLowerInvariant(),
						"field name mismatch");

					Assert.AreEqual(
						variable.Description.Size,
						(int) Marshal.SizeOf(field.FieldType),
						"field size mismatch: " + field.Name);

					Assert.AreEqual(
						variable.Description.StartOffset,
						(int) Marshal.OffsetOf<ConstantsStruct>(field.Name),
						"field offset mismatch: " + field.Name);
				}
			}
		}
	}
}
