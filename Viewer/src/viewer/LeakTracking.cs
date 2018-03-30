using SharpDX;
using SharpDX.Diagnostics;
using System.Collections.Generic;
using System.Diagnostics;

public static class LeakTracking {
	private static string GetStackTrace() {
		return new StackTrace(4, false).ToString();
	}

	[Conditional("LEAKTRACKING")]
	public static void Setup() {
		Configuration.EnableObjectTracking = true;
		ObjectTracker.StackTraceProvider = GetStackTrace;

		HashSet<ComObject> trackedObjects = new HashSet<ComObject>();

		ObjectTracker.Tracked += (sender, eventArgs) => {
			trackedObjects.Add(eventArgs.Object);
		};
		ObjectTracker.UnTracked += (sender, eventArgs) => {
			trackedObjects.Remove(eventArgs.Object);
		};
	}

	[Conditional("LEAKTRACKING")]
	public static void Finish() {
		if (ObjectTracker.FindActiveObjects().Count > 0) {
			Trace.WriteLine(ObjectTracker.ReportActiveObjects());
		} else {
			Trace.WriteLine("Zero leaked objects.");
		}
	}
}
