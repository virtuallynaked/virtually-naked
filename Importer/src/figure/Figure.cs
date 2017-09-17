using SharpDX;
using System.Collections.Generic;
using System.Linq;
using System;

public class Figure {
	private readonly string name;
	private readonly Figure parent;
	private readonly FigureRecipe recipe;
	private readonly Geometry geometry;
	private readonly ChannelSystem channelSystem;
	private readonly BoneSystem boneSystem;
	private readonly Morpher morpher;
	private readonly Automorpher automorpher;
	private readonly SkinBinding skinBinding;
	private readonly Dictionary<string, UvSet> uvSets;
	private readonly UvSet defaultUvSet;
	
	public Figure(string name, Figure parent, FigureRecipe recipe, Geometry geometry, ChannelSystem channelSystem, BoneSystem boneSystem, Morpher morpher, Automorpher automorpher, SkinBinding skinBinding, Dictionary<string, UvSet> uvSets, UvSet defaultUvSet) {
		this.name = name;
		this.parent = parent;
		this.recipe = recipe;
		this.geometry = geometry;
		this.channelSystem = channelSystem;
		this.boneSystem = boneSystem;
		this.morpher = morpher;
		this.automorpher = automorpher;
		this.skinBinding = skinBinding;
		this.uvSets = uvSets;
		this.defaultUvSet = defaultUvSet;
	}
	
	public string Name => name;
	public Figure Parent => parent;
	public int VertexCount => geometry.VertexCount;
	public ChannelSystem ChannelSystem => channelSystem;
	public BoneSystem BoneSystem => boneSystem;
	public Geometry Geometry => geometry;
	public Morpher Morpher => morpher;
	public Automorpher Automorpher => automorpher;
	public SkinBinding SkinBinding => skinBinding;
	public Dictionary<string, UvSet> UvSets => uvSets;
	public UvSet DefaultUvSet => defaultUvSet;

	/*
	 * Recipes
	 */

	public ChannelSystemRecipe MakeChannelSystemRecipe() {
		return new ChannelSystemRecipe(recipe.Channels, recipe.Formulas);
	}

	public BoneSystemRecipe MakeBoneSystemRecipe() {
		return new BoneSystemRecipe(recipe.Bones);
	}
	
	/*
	 * Channel System
	 */
	
	public List<Channel> Channels => channelSystem.Channels;
	public Dictionary<string, Channel> ChannelsByName => channelSystem.ChannelsByName;
		
	public ChannelInputs MakeDefaultChannelInputs() {
		return channelSystem.MakeDefaultChannelInputs();
	}
	
	public ChannelInputs MakeZeroChannelInputs() {
		return channelSystem.MakeZeroChannelInputs();
	}

	public ChannelOutputs Evaluate(ChannelOutputs parentOutputs, ChannelInputs inputs) {
		return channelSystem.Evaluate(parentOutputs, inputs);
	}

	/*
	 * Bone System
	 */

	public List<Bone> Bones => boneSystem.Bones;
	public Dictionary<string, Bone> BonesByName => boneSystem.BonesByName;
	public Bone RootBone => boneSystem.RootBone;
	
	public StagedSkinningTransform[] GetBoneTransforms(ChannelOutputs outputs) {
		return boneSystem.GetBoneTransforms(outputs);
	}

	/*
	 * Shaping System
	 */

	public ShaperParameters MakeShaperParameters(bool[] channelsToInclude) {
		return new ShaperParameters(
			Geometry.VertexPositions,
			Morpher.Morphs.Count,
			Morpher.Morphs.Select(morph => morph.Channel.Index).ToArray(),
			Morpher.ConvertToVertexDeltas(VertexCount, channelsToInclude),
			Automorpher?.BaseDeltaWeights,
			SkinBinding.Bones.Count,
			SkinBinding.Bones.Select(bone => bone.Index).ToArray(),
			SkinBinding.BoneWeights);
	}
		
	public Vector3[] CalculateControlPositions(ChannelOutputs channelOutputs, Vector3[] baseDeltas) {
		Vector3[] controlVertices = Geometry.VertexPositions.Select(p => p).ToArray();
		Morpher.Apply(channelOutputs, controlVertices);
		automorpher?.Apply(baseDeltas, controlVertices);
		StagedSkinningTransform[] boneTransforms = boneSystem.GetBoneTransforms(channelOutputs);
		SkinBinding.Apply(boneTransforms, controlVertices);
		return controlVertices;
	}

	public Vector3[] CalculateDeltas(ChannelOutputs channelOutputs) {
		Vector3[] controlVertices = new Vector3[Geometry.VertexCount];
		Morpher.Apply(channelOutputs, controlVertices);
		return controlVertices;
	}
	
	public InverterParameters MakeInverterParameters() {
		return new InverterParameters(Geometry.Faces, Geometry.FaceGroupMap, Geometry.FaceGroupNames, SkinBinding.FaceGroupToNodeMap);
	}
}
