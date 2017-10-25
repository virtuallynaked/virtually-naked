using SharpDX;
using System;
using System.Collections.Generic;

namespace FlatIk {
	public class Bone {
		public Bone Parent { get; }
		public Vector2 Center { get; }
		public Vector2 End { get; }
		public float Rotation { get; set; }

		public Bone(Bone parent, Vector2 center, Vector2 end, float rotation) {
			Parent = parent;
			Center = center;
			End = end;
			Rotation = rotation;
		}
		
		public static Bone MakeWithOffset(Bone parent, Vector2 endOffset, float rotation) {
			Vector2 center = parent != null ? parent.End : Vector2.Zero;
			Vector2 end = center + endOffset;
			return new Bone(parent, center, end, rotation);
		}
		
		public Matrix3x2 GetLocalTransform() {
			return Matrix3x2.Rotation(Rotation, Center);
		}

		public Matrix3x2 GetChainedTransform() {
			Matrix3x2 parentTransform = Parent != null ? Parent.GetChainedTransform() : Matrix3x2.Identity;
			return GetLocalTransform() * parentTransform;
		}
	}
}