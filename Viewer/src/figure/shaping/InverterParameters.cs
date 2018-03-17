using System.Collections.Generic;

public class InverterParameters {
	public Quad[] ControlFaces { get; }
	public int[] ControlFaceToBoneMap { get; }
	public BoneAttributes[] BoneAttributes { get; }
	
	public InverterParameters(Quad[] controlFaces, int[] controlFaceToBoneMap, BoneAttributes[] boneAttributes) {
		ControlFaces = controlFaces;
		ControlFaceToBoneMap = controlFaceToBoneMap;
		BoneAttributes = boneAttributes;
	}
}
