using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public class UberMaterialTest {
	[TestMethod]
	public void TestConstantsStructMatchesShader() {
		MaterialTestCommon.TestConstantsStructMatchesShader<UberMaterial, UberConstants>(UberMaterial.StandardShaderName, "cbuffer0");
		MaterialTestCommon.TestConstantsStructMatchesShader<UberMaterial, UberConstants>(UberMaterial.UnorderedTransparencyShaderName, "cbuffer0");
	}
}
