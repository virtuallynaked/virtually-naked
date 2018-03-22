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
		private readonly SkeletonInputs inputs;
		private readonly IIkSolver solver;
		private Vector2 target = new Vector2(5, 5);
		
		private static List<Bone> MakeStandardBones() {
			float l = (float) Math.Sqrt(0.5);

			float rotationLimit = MathUtil.Pi / 3;

			var bone0 = Bone.MakeWithOffset(0, null, Vector2.UnitY, MathUtil.Pi);
			var bone1 = Bone.MakeWithOffset(1, bone0, Vector2.UnitY, rotationLimit);
			var bone2 = Bone.MakeWithOffset(2, bone1, Vector2.UnitY, rotationLimit);
			var bone3 = Bone.MakeWithOffset(3, bone2, Vector2.UnitY, rotationLimit);
			var bone4 = Bone.MakeWithOffset(4, bone3, Vector2.UnitY, rotationLimit);
			var bone5 = Bone.MakeWithOffset(5, bone4, Vector2.UnitX, rotationLimit);
			var bone6 = Bone.MakeWithOffset(6, bone5, Vector2.UnitX, rotationLimit);
			var bone7 = Bone.MakeWithOffset(7, bone6, Vector2.UnitX, rotationLimit);
			var bone8 = Bone.MakeWithOffset(8, bone7, Vector2.UnitX, rotationLimit);
			
			return new List<Bone> { bone0, bone1, bone2, bone3, bone4, bone5, bone6, bone7, bone8 };
		}
		
		public FlatIkApp() {
			renderEnvironment = new WindowedDirect2dRenderEnvironment("FlatIkApp", false);
			context = renderEnvironment.D2dContext;
			whiteBrush = new SolidColorBrush(context, Color.White);
			redBrush = new SolidColorBrush(context, Color.Red);

			bones = MakeStandardBones();
			inputs = new SkeletonInputs(bones.Count);

			solver = new FabrIkSolver();
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
			var unposedSource = sourceBone.End;

			solver.DoIteration(inputs, sourceBone, unposedSource, target);
		}

		private Matrix3x2 GetWorldToFormTransform() {
			float worldExtent = 20;
			var size = renderEnvironment.Size;
			float scaling = Math.Min(size.Width, size.Height) / worldExtent;
			return new Matrix3x2(1, 0, 0, -1, 0, 0) * Matrix3x2.Scaling(scaling) * Matrix3x2.Translation(size.Width / 2, size.Height / 2);
		}

		private void Render() {
			context.Clear(null);

			Matrix3x2 worldToFormTransform = GetWorldToFormTransform();

			foreach (var bone in bones) {
				var transform = bone.GetChainedTransform(inputs) * worldToFormTransform;
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
