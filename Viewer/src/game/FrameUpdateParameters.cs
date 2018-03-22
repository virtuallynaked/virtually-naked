using SharpDX;
using Valve.VR;

public class FrameUpdateParameters {
	public float Time { get; }
	public float TimeDelta { get; }
	public TrackedDevicePose_t[] GamePoses { get; }
	public Vector3 HeadPosition { get; }

	public FrameUpdateParameters(float time, float timeDelta, TrackedDevicePose_t[] gamePoses, Vector3 headPosition) {
		Time = time;
		TimeDelta = timeDelta;
		GamePoses = gamePoses;
		HeadPosition = headPosition;
	}
}
