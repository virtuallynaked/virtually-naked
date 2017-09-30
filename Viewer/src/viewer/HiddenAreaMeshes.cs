using SharpDX.Direct3D11;
using System;
using Valve.VR;

class HiddenAreaMeshes : IDisposable {
	private readonly HiddenAreaMesh leftEyeMesh;
	private readonly HiddenAreaMesh rightEyeMesh;

	public HiddenAreaMeshes(Device device) {
		leftEyeMesh = HiddenAreaMesh.Make(device, EVREye.Eye_Left);
		rightEyeMesh = HiddenAreaMesh.Make(device, EVREye.Eye_Right);
	}

	public void Dispose() {
		leftEyeMesh?.Dispose();
		rightEyeMesh?.Dispose();
	}

	public HiddenAreaMesh GetMesh(EVREye eye) {
		return eye == EVREye.Eye_Left ? leftEyeMesh : rightEyeMesh;
	}
}
