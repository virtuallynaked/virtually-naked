using System.Diagnostics;
using System.Threading;

static class DebugUtilities {
	public static void Burn(long ms) {
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (stopwatch.ElapsedMilliseconds < ms) {
		}
	}

	public static void Sleep(long ms) {
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (stopwatch.ElapsedMilliseconds < ms) {
			Thread.Yield();
		}
	}
}

