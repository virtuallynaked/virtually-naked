using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class FigureRecipe {
	public String Name { get; set; }
	public GeometryRecipe Geometry { get; set; }
	public List<ChannelRecipe> Channels { get; set; } = new List<ChannelRecipe>();
	public List<FormulaRecipe> Formulas { get; set; } = new List<FormulaRecipe>();
	public List<BoneRecipe> Bones { get; set; } = new List<BoneRecipe>();
	public List<MorphRecipe> Morphs { get; set; } = new List<MorphRecipe>();
	public AutomorpherRecipe Automorpher { get; set; }
	public SkinBindingRecipe SkinBinding { get; set; }
	public List<UvSetRecipe> UvSets { get; set; } = new List<UvSetRecipe>();

	private static RigidTransform[] MakeChildToParentBindPoseTransforms(
		ChannelSystem channelSystem,
		BoneSystem selfBoneSystem,
		BoneSystem parentBoneSystem) {
		var childToParentBindPoseTransforms = parentBoneSystem.Bones
			.Select(parentBone => {
				selfBoneSystem.BonesByName.TryGetValue(parentBone.Name, out var childBone);
				if (childBone == null) {
					return RigidTransform.Identity;
				}

				var originToChildBindPoseTransform = childBone.GetOriginToBindPoseTransform(channelSystem.DefaultOutputs);
				var originToParentBindPoseTransform = parentBone.GetOriginToBindPoseTransform(channelSystem.Parent.DefaultOutputs);
				
				var childBonePoseToParentBindPoseTransform = 
					originToChildBindPoseTransform.Invert().Chain(originToParentBindPoseTransform);

				return childBonePoseToParentBindPoseTransform;
			})
			.ToArray();
		return childToParentBindPoseTransforms;
	}

	public Figure Bake(ContentFileLocator fileLocator, Figure parentFigure) {
		if (Channels == null) {
			Channels = new List<ChannelRecipe>();
		}
		if (Formulas == null) {
			Formulas = new List<FormulaRecipe>();
		}
		if (Morphs == null) {
			Morphs = new List<MorphRecipe>();
		}

		Geometry geometry = Geometry.Bake();

		Dictionary<string, UvSet> uvSets = new Dictionary<string, UvSet>();
		UvSets.ForEach(recipe => recipe.Bake(geometry, uvSets));
		UvSet defaultUvSet = uvSets[Geometry.DefaultUvSet];
		
		ChannelSystem channelSystem = new ChannelSystemRecipe(Channels, Formulas).Bake(parentFigure?.ChannelSystem);
				
		int graftVertexOffset = Geometry.VertexPositions.Length;

		List<MorphRecipe> rewrittenMorphRecipes = Automorpher != null ? Automorpher.Rewrite(Morphs, parentFigure) : Morphs;
		List<Morph> morphs = rewrittenMorphRecipes.Select(recipe => recipe.Bake(fileLocator, channelSystem.ChannelsByName)).ToList();
		Morpher morpher = new Morpher(morphs);

		Automorpher automorpher = Automorpher?.Bake();

		BoneSystem selfBoneSystem = new BoneSystemRecipe(Bones).Bake(channelSystem.ChannelsByName);

		BoneSystem boneSystem;
		RigidTransform[] childToParentBindPoseTransforms;
		if (parentFigure != null) {
			boneSystem = parentFigure.BoneSystem;
			childToParentBindPoseTransforms = MakeChildToParentBindPoseTransforms(channelSystem, selfBoneSystem, boneSystem);
		} else {
			boneSystem = selfBoneSystem;
			childToParentBindPoseTransforms = null;
		}
		
		SkinBinding skinBinding = SkinBinding.Bake(boneSystem.BonesByName, selfBoneSystem.BonesByName);

		OcclusionBinding occlusionBinding = OcclusionBinding.MakeForFigure(Name, geometry, boneSystem, skinBinding);
		
		return new Figure(Name, parentFigure, this, geometry, channelSystem, boneSystem, childToParentBindPoseTransforms, morpher, automorpher, skinBinding, uvSets, defaultUvSet, occlusionBinding);
	}
}
