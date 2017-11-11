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

		/**
		 *  Given a point that has already had bone total-transform applied to it, retransform it as the rotation of this bone was adjusted by a delta.
		 */
		public Vector2 RetransformPoint(float rotationDelta, Vector2 point) {
			Matrix3x2 parentTransform = Parent != null ? Parent.GetChainedTransform() : Matrix3x2.Identity;
			Vector2 transformedCenter = Matrix3x2.TransformPoint(parentTransform, Center);

			var retransform = Matrix3x2.Rotation(rotationDelta, transformedCenter);
			return Matrix3x2.TransformPoint(retransform, point);
		}

		/**
		 * Returns the gradient of a transformed point with respect to the rotation parameter.
		 */
		public Vector2 GetGradientOfTransformedPointWithRespectToRotation(Vector2 point) {
			Matrix3x2 parentTransform = Parent != null ? Parent.GetChainedTransform() : Matrix3x2.Identity;
			Vector2 transformedCenter = Matrix3x2.TransformPoint(parentTransform, Center);

			Vector2 centeredPoint = point - transformedCenter;
			return new Vector2(-centeredPoint.Y, centeredPoint.X);

		}
	}
}