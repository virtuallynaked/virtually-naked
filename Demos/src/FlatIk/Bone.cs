using SharpDX;

namespace FlatIk {
	public class Bone {
		public int Index { get; }
		public Bone Parent { get; }
		public Vector2 Center { get; }
		public Vector2 End { get; }

		public Bone(int index, Bone parent, Vector2 center, Vector2 end) {
			Index = index;
			Parent = parent;
			Center = center;
			End = end;
		}
		
		public static Bone MakeWithOffset(int index, Bone parent, Vector2 endOffset) {
			Vector2 center = parent != null ? parent.End : Vector2.Zero;
			Vector2 end = center + endOffset;
			return new Bone(index, parent, center, end);
		}
		
		public float GetRotation(SkeletonInputs inputs) {
			return inputs.GetRotation(Index);
		}
		
		public void SetRotation(SkeletonInputs inputs, float rotation) {
			inputs.SetRotation(Index, rotation);
		}

		public void IncrementRotation(SkeletonInputs inputs, float rotationDelta) {
			inputs.IncrementRotation(Index, rotationDelta);
		}

		public Matrix3x2 GetLocalTransform(SkeletonInputs inputs) {
			float rotation = GetRotation(inputs);
			return Matrix3x2.Rotation(rotation, Center);
		}

		private static Matrix3x2 GetChainedTransform(Bone bone, SkeletonInputs inputs) {
			if (bone == null) {
				return Matrix3x2.Translation(inputs.Translation);
			} else {
				return bone.GetChainedTransform(inputs);
			}
		}

		public Matrix3x2 GetChainedTransform(SkeletonInputs inputs) {
			Matrix3x2 parentTransform = GetChainedTransform(Parent, inputs);
			return GetLocalTransform(inputs) * parentTransform;
		}

		/**
		 *  Given a point that has already had bone total-transform applied to it, retransform it as the rotation of this bone was adjusted by a delta.
		 */
		public Vector2 RetransformPoint(SkeletonInputs inputs, float rotationDelta, Vector2 point) {
			Matrix3x2 parentTransform = GetChainedTransform(Parent, inputs);
			Vector2 transformedCenter = Matrix3x2.TransformPoint(parentTransform, Center);

			var retransform = Matrix3x2.Rotation(rotationDelta, transformedCenter);
			return Matrix3x2.TransformPoint(retransform, point);
		}

		/**
		 * Returns the gradient of a transformed point with respect to the rotation parameter.
		 */
		public Vector2 GetGradientOfTransformedPointWithRespectToRotation(SkeletonInputs inputs, Vector2 point) {
			Matrix3x2 parentTransform = GetChainedTransform(Parent, inputs);
			Vector2 transformedCenter = Matrix3x2.TransformPoint(parentTransform, Center);

			Vector2 centeredPoint = point - transformedCenter;
			return new Vector2(-centeredPoint.Y, centeredPoint.X);
		}
	}
}