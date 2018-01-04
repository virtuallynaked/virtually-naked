using System.Collections.Generic;

public class InverterParameters {
	public Quad[] ControlFaces { get; }
	public int[] FaceGroupMap { get; }
	public string[] FaceGroupNames { get; }
	public Dictionary<string, string> FaceGroupToNodeMap { get; }
	public BoneAttributes[] BoneAttributes { get; }
	
	public InverterParameters(Quad[] controlFaces, int[] faceGroupMap, string[] faceGroupNames, Dictionary<string, string> faceGroupToNodeMap, BoneAttributes[] boneAttributes) {
		ControlFaces = controlFaces;
		FaceGroupMap = faceGroupMap;
		FaceGroupNames = faceGroupNames;
		FaceGroupToNodeMap = faceGroupToNodeMap;
		BoneAttributes = boneAttributes;
	}
}
