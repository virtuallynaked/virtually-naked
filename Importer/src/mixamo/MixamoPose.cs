using SharpDX;
using System.Collections.Generic;

public class MixamoPose {
	public Dictionary<string, Quaternion> JointRotations {get; }
	public Vector3 RootTranslation {get; }

	public MixamoPose(Dictionary<string, Quaternion> jointRotations, Vector3 rootTranslation) {
		JointRotations = jointRotations;
		RootTranslation = rootTranslation;
	}
}
