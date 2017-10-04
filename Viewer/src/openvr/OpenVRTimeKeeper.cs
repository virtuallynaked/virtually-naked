using System.Diagnostics;
using Valve.VR;

public class OpenVRTimeKeeper {
	private const float MaxTimeDelta = 0.1f;

	private float nextFrameTime;
	private float timeDelta;
	
	private long initialTimestamp;
	
	public float NextFrameTime => nextFrameTime;
	public float TimeDelta => timeDelta;

	public void Start() {
		initialTimestamp = Stopwatch.GetTimestamp();
	}

	public void AdvanceFrame() {
		float previousFrameTime = nextFrameTime;

		long currentTimestamp = Stopwatch.GetTimestamp();
		float timeRemaining = OpenVR.Compositor.GetFrameTimeRemaining();

		double currentTime = (double) (currentTimestamp - initialTimestamp) / Stopwatch.Frequency;

		nextFrameTime = (float) (currentTime + timeRemaining);
		timeDelta = MathExtensions.Clamp(nextFrameTime - previousFrameTime, 0, MaxTimeDelta);
	}
}
