using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;

namespace FlatIk {
	public class FlatIkApp : IDemoApp, IDisposable {
		private readonly WindowedDirect2dRenderEnvironment renderEnvironment;
		private readonly DeviceContext context;
		private readonly Brush whiteBrush;
		private readonly Brush redBrush;

		private readonly List<Bone> bones;
		private readonly IIkSolver solver;
		private Vector2 target = new Vector2(2, 1);
		
		private static List<Bone> MakeStandardBones() {
			var bone0 = Bone.MakeWithOffset(null, Vector2.UnitX, +MathUtil.PiOverFour);
			var bone1 = Bone.MakeWithOffset(bone0, Vector2.UnitX, +MathUtil.PiOverTwo);
			var bone2 = Bone.MakeWithOffset(bone1, Vector2.UnitX, -MathUtil.PiOverTwo);
			var bone3 = Bone.MakeWithOffset(bone2, Vector2.UnitX, -MathUtil.PiOverTwo);
			var bone4 = Bone.MakeWithOffset(bone3, Vector2.UnitX, +MathUtil.PiOverTwo);

			return new List<Bone> { bone0, bone1, bone2, bone3, bone4 };
		}

		public FlatIkApp() {
			renderEnvironment = new WindowedDirect2dRenderEnvironment("FlatIkApp", false);
			context = renderEnvironment.D2dContext;
			whiteBrush = new SolidColorBrush(context, Color.White);
			redBrush = new SolidColorBrush(context, Color.Red);

			bones = MakeStandardBones();
			solver = new GaussNewtonIkSolver(bones);
		}

		public void Dispose() {
			whiteBrush.Dispose();
			renderEnvironment.Dispose();
		}

		public void Run() {
			renderEnvironment.Form.MouseClick += (sender, e) => {
				Vector2 formPosition = new Vector2(e.X, e.Y);
				var transform = GetWorldToFormTransform();
				transform.Invert();
				Vector2 worldPosition = Matrix3x2.TransformPoint(transform, formPosition);
				target = worldPosition;
			};

			renderEnvironment.Form.KeyPress += (sender, e) => {
				if (e.KeyChar == ' ') {
					DoIkIteration();
				}
			};

			renderEnvironment.Run(Render);
		}

		private void DoIkIteration() {
			var sourceBone = bones[bones.Count - 1];
			var source = Matrix3x2.TransformPoint(sourceBone.GetChainedTransform(), sourceBone.End);

			solver.DoIteration(source, target);
		}

		private Matrix3x2 GetWorldToFormTransform() {
			float worldExtent = 10;
			var size = renderEnvironment.Size;
			float scaling = Math.Min(size.Width, size.Height) / worldExtent;
			return new Matrix3x2(1, 0, 0, -1, 0, 0) * Matrix3x2.Scaling(scaling) * Matrix3x2.Translation(size.Width / 2, size.Height / 2);
		}

		private void Render() {
			context.Clear(null);

			Matrix3x2 worldToFormTransform = GetWorldToFormTransform();

			foreach (var bone in bones) {
				var transform = bone.GetChainedTransform() * worldToFormTransform;
				var formCenter = Matrix3x2.TransformPoint(transform, bone.Center);
				var formEnd = Matrix3x2.TransformPoint(transform, bone.End);

				context.DrawEllipse(new Ellipse(formCenter, 5, 5), whiteBrush, 2);
				context.DrawLine(formCenter, formEnd, whiteBrush, 2);
			}

			var formTarget = Matrix3x2.TransformPoint(worldToFormTransform, target);

			float crossSize = 5;
			context.DrawLine(
				formTarget + crossSize * new Vector2(-1, -1),
				formTarget + crossSize * new Vector2(+1, +1),
				redBrush, 2);
			context.DrawLine(
				formTarget + crossSize * new Vector2(-1, +1),
				formTarget + crossSize * new Vector2(+1, -1),
				redBrush, 2);
		}
	}
}