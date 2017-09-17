using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;

public class CollisionTree {
	public static CollisionTree Make(Vector3[] points) {
		List<int> indices = Enumerable.Range(0, points.Length).ToList();
		var root = MakeNode(points, indices);
		return new CollisionTree(root);
	}

	private static CollisionTreeNode MakeNode(Vector3[] points, List<int> indices) {
		if (indices.Count == 0) {
			return null;
		}
		
		var boundingBox = BoundingBox.FromPoints(indices.Select(idx => points[idx]).ToArray());
		
		var dimensions = boundingBox.Maximum - boundingBox.Minimum;
		int widestDimensionIdx = 0;
		if (dimensions[1] > dimensions[widestDimensionIdx]) {
			widestDimensionIdx = 1;
		}
		if (dimensions[2] > dimensions[widestDimensionIdx]) {
			widestDimensionIdx = 2;
		}

		if (dimensions[widestDimensionIdx] == 0) {
			//bounding box is a single point, so don't try to split it
			return new CollisionTreeNode(boundingBox, indices, null, null);
		}
		
		float splitPosition = (boundingBox.Minimum[widestDimensionIdx] + boundingBox.Maximum[widestDimensionIdx]) / 2;
		var leftIndices = new List<int>(indices.Count);
		var rightIndices = new List<int>(indices.Count);
		foreach (int index in indices) {
			if (points[index][widestDimensionIdx] < splitPosition) {
				leftIndices.Add(index);
			} else {
				rightIndices.Add(index);
			}
		}

		var leftNode = MakeNode(points, leftIndices);
		var rightNode = MakeNode(points, rightIndices);
		return new CollisionTreeNode(boundingBox, indices, leftNode, rightNode);
	}
	
	private class CollisionTreeNode {
		private BoundingBox boundingBox;
		private readonly List<int> indices;
		private readonly CollisionTreeNode leftNode;
		private readonly CollisionTreeNode rightNode;

		public CollisionTreeNode(BoundingBox boundingBox, List<int> indices, CollisionTreeNode leftNode, CollisionTreeNode rightNode) {
			if (leftNode == null || rightNode == null) {
				if (leftNode != null || rightNode != null) {
					throw new ArgumentException("either both branches must be null, or neither");
				}
				if (boundingBox.Maximum != boundingBox.Minimum) {
					throw new ArgumentException("bounding box must be a single point when branches are null");
				}
			}

			this.boundingBox = boundingBox;
			this.indices = indices;
			this.leftNode = leftNode;
			this.rightNode = rightNode;
		}

		public void AccumulatePointsInSphere(BoundingSphere containmentSphere, List<int> list) {
			var containmentType = containmentSphere.Contains(ref boundingBox);
			if (containmentType == ContainmentType.Contains) {
				list.AddRange(indices);
			} else if (containmentType == ContainmentType.Intersects) {
				//note: left and right

				leftNode.AccumulatePointsInSphere(containmentSphere, list);
				rightNode.AccumulatePointsInSphere(containmentSphere, list);
			} else if (containmentType == ContainmentType.Disjoint) {
				//do nothing
			} else {
				throw new Exception("unknown containment type");
			}
		}
	}
		
	private readonly CollisionTreeNode root;

	private CollisionTree(CollisionTreeNode root) {
		this.root = root;
	}

	public List<int> GetPointsInSphere(BoundingSphere containmentSphere) {
		List<int> list = new List<int>();
		root?.AccumulatePointsInSphere(containmentSphere, list);
		return list;
	}
}


