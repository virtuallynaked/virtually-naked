using SharpDX;
using SharpDX.Direct2D1;
using System;

namespace FlatIk {
	public class FlatIkApp : IDemoApp, IDisposable {
		private readonly WindowedDirect2dRenderEnvironment renderEnvironment;
		private readonly DeviceContext context;

		public FlatIkApp() {
			renderEnvironment = new WindowedDirect2dRenderEnvironment("FlatIkApp", false);
			context = renderEnvironment.D2dContext;
		}

		public void Dispose() {
			renderEnvironment.Dispose();
		}

		public void Run() {
			renderEnvironment.Run(Render);
		}

		private void Render() {
			context.Clear(null);
			
			using (var brush = new SolidColorBrush(context, Color.White)) {
				context.DrawEllipse(new Ellipse(new Vector2(50, 50), 10, 10), brush, 5);
			}
		}
	}
}