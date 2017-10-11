using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class FigureRecipe {
	public GeometryRecipe Geometry { get; set; }
	public List<ChannelRecipe> Channels { get; set; } = new List<ChannelRecipe>();
	public List<FormulaRecipe> Formulas { get; set; } = new List<FormulaRecipe>();
	public List<BoneRecipe> Bones { get; set; } = new List<BoneRecipe>();
	public List<MorphRecipe> Morphs { get; set; } = new List<MorphRecipe>();
	public AutomorpherRecipe Automorpher { get; set; }
	public SkinBindingRecipe SkinBinding { get; set; }
	public List<UvSetRecipe> UvSets { get; set; } = new List<UvSetRecipe>();

	public Figure Bake(string name, Figure parentFigure) {
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
		List<Morph> morphs = rewrittenMorphRecipes.Select(recipe => recipe.Bake(channelSystem.ChannelsByName)).ToList();
		Morpher morpher = new Morpher(morphs);

		Automorpher automorpher = Automorpher?.Bake();

		BoneSystem selfBoneSystem = new BoneSystemRecipe(Bones).Bake(channelSystem.ChannelsByName);
		BoneSystem boneSystem = parentFigure?.BoneSystem ?? selfBoneSystem;
				
		SkinBinding skinBinding = SkinBinding.Bake(boneSystem.BonesByName, selfBoneSystem.BonesByName);

		OcclusionBinding occlusionBinding = OcclusionBinding.MakeForFigure(name, geometry, boneSystem, skinBinding);
		
		return new Figure(name, parentFigure, this, geometry, channelSystem, boneSystem, morpher, automorpher, skinBinding, uvSets, defaultUvSet, occlusionBinding);
	}
}
