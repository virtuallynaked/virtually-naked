using SharpDX;
using System;

namespace FlatIk {
	public class SkeletonInputs {
		public Vector2 Translation { get; set; }
		private float[] rotations;

		public SkeletonInputs(int boneCount) {
			rotations = new float[boneCount];
		}

		public void SetRotation(int boneIdx, float value) {
			rotations[boneIdx] = (float) Math.IEEERemainder(value, Math.PI * 2);
		}

		public float GetRotation(int boneIdx) {
			return rotations[boneIdx];
		}

		public void IncrementRotation(int boneIdx, float delta) {
			SetRotation(boneIdx, GetRotation(boneIdx) + delta);
		}
	}
}
