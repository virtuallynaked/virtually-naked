using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;

[TestClass]
public class HdMorphTest {
	[TestMethod]
	public void TestPackPath() {
		int[] path = { 0, 2, 1 };
		uint packedPath = HdMorph.VertexEdit.PackPath(0, 2, 1);
		var vertexEdit = new HdMorph.VertexEdit(packedPath, Vector3.Zero);

		Assert.AreEqual(path.Length, vertexEdit.PathLength);
		Assert.AreEqual(path[0], vertexEdit.GetPathElement(0));
		Assert.AreEqual(path[1], vertexEdit.GetPathElement(1));
		Assert.AreEqual(path[2], vertexEdit.GetPathElement(2));
	}
}
