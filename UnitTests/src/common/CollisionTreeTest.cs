using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;

[TestClass]
public class CollisionTreeTest {
	[TestMethod]
	public void TestCollisionTree() {
		var random = new Random(0);
		Vector3[] points = Enumerable.Range(0, 100)
			.Select(idx => {
				float x = (float) random.NextDouble();
				float y = (float) random.NextDouble();
				float z = (float) random.NextDouble();
				return new Vector3(x, y, z);
			})
			.ToArray();

		var tree = CollisionTree.Make(points);
		var sphere = new BoundingSphere(new Vector3(0.3f, 0.1f, 0.5f), 0.5f);
		
		var indices = tree.GetPointsInSphere(sphere);
		Assert.IsTrue(indices.Count > 0 && indices.Count < points.Length);

		var indexSet = new HashSet<int>(indices);

		for (int idx = 0; idx < points.Length; ++idx) {
			bool expectedIsInSphere = sphere.Contains(ref points[idx]) == ContainmentType.Contains;
			bool actualIsInSphere = indexSet.Contains(idx);
			Assert.AreEqual(expectedIsInSphere, actualIsInSphere);
		}
	}
}
