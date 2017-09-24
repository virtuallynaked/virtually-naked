using SharpDX;

public class FrameUpdateParameters {
	public float Time { get; }
	public float TimeDelta { get; }
	public Vector3 HeadPosition { get; }

	public FrameUpdateParameters(float time, float timeDelta, Vector3 headPosition) {
		Time = time;
		TimeDelta = timeDelta;
		HeadPosition = headPosition;
	}
}
