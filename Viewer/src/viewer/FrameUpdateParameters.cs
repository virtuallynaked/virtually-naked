using SharpDX;

public class FrameUpdateParameters {
	public float Time { get; }
	public float TimeDelta { get; }

	public FrameUpdateParameters(float time, float timeDelta) {
		Time = time;
		TimeDelta = timeDelta;
	}
}
