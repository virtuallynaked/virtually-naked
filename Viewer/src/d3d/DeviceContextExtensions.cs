using SharpDX.Direct3D11;
using System;

public static class DeviceContextExtensions {
	public static void WithEvent(this DeviceContext context, string name, Action action) {
		using (UserDefinedAnnotation annotator = context.QueryInterface<UserDefinedAnnotation>()) {
			annotator.BeginEvent(name);
			try {
				action();
			} finally {
				annotator.EndEvent();
			}
		}
	}

	public static void SetMarker(this DeviceContext context, string name) {
		using (UserDefinedAnnotation annotator = context.QueryInterface<UserDefinedAnnotation>()) {
			annotator.SetMarker(name);
		}
	}
}
