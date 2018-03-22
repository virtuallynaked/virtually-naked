using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public class BasicSpecularMaterialTest {
	[TestMethod]
	public void TestConstantsStructMatchesShader() {
		MaterialTestCommon.TestConstantsStructMatchesShader<BasicSpecularMaterial, BasicSpecularMaterial.Constants>(BasicSpecularMaterial.ShaderName, "cbuffer0");
	}
}
