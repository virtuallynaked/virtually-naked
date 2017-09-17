using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;

public class FigureRecipeMerger {
	public struct Offset {
		public int Vertex { get; }
		public int Face { get; }
		public int Surface { get; }

		public static readonly Offset Zero = new Offset(0, 0, 0);

		public Offset(int vertex, int face, int surface) {
			this.Vertex = vertex;
			this.Face = face;
			this.Surface = surface;
		}

		public Offset Add(int vertex, int face, int surface) {
			return new Offset(Vertex + vertex, Face + face, Surface + surface);
		}
	}

	public class Reindexer {
		public static Reindexer Make(FigureRecipe parent, FigureRecipe[] children) {
			int childCount = children.Length;

			//build hidden face set
			int parentFaceCount = parent.Geometry.Faces.Length;
			bool[] hiddenFaceSet = new bool[parentFaceCount];
			for (int childIdx = 0; childIdx < children.Length; ++childIdx) {
				Graft graft = children[childIdx].Geometry.Graft;
				if (graft != null) {
					foreach (int hiddenFace in graft.HiddenFaces) {
						hiddenFaceSet[hiddenFace] = true;
					}
				}
			}

			//build parent face map
			int[] parentFaceMap = new int[parent.Geometry.Faces.Length];
			int nonHiddenParentFaceCount = 0;
			for (int faceIdx = 0; faceIdx < parentFaceCount; ++faceIdx) {
				if (hiddenFaceSet[faceIdx]) {
					parentFaceMap[faceIdx] = -1;
				} else {
					parentFaceMap[faceIdx] = nonHiddenParentFaceCount;
					nonHiddenParentFaceCount += 1;
				}
			}

			//build child offsets and totals
			Offset[] offsets = new Offset[childCount];
			Offset currentOffset = new Offset(
				parent.Geometry.GetVertexCount(),
				nonHiddenParentFaceCount,
				parent.Geometry.GetSurfaceCount()
			);
			for (int i = 0; i < childCount; ++i) {
				offsets[i] = currentOffset;

				FigureRecipe currentFigure = children[i];
				currentOffset = currentOffset.Add(
					currentFigure.Geometry.GetVertexCount(),
					currentFigure.Geometry.Faces.Length,
					currentFigure.Geometry.GetSurfaceCount());
			}
			Offset totals = currentOffset;
						
			return new Reindexer(parentFaceMap, offsets, totals);
		}

		private readonly int[] parentFaceMap;
		private readonly Offset[] childOffsets;
		private readonly Offset totals;

		public Reindexer(int[] parentFaceMap, Offset[] childOffsets, Offset totals) {
			this.parentFaceMap = parentFaceMap;
			this.childOffsets = childOffsets;
			this.totals = totals;
		}

		public Offset[] ChildOffsets => childOffsets;
		public Offset Totals => totals;

		public int GetParentVertexCount() {
			if (childOffsets.Length == 0) {
				return totals.Vertex;
			} else {
				return childOffsets[0].Vertex;
			}
		}

		public int GetChildVertexCount(int childIdx) {
			if (childIdx + 1 == childOffsets.Length) {
				return totals.Vertex - childOffsets[childIdx].Vertex;
			} else {
				return childOffsets[childIdx + 1].Vertex - childOffsets[childIdx].Vertex;
			}
		}

		public int RemapParentFaceIdx(int faceIdx) {
			return parentFaceMap[faceIdx];
		}

		public bool IsParentFaceHidden(int faceIdx) {
			return parentFaceMap[faceIdx] == -1;
		}
	}

	private readonly FigureRecipe parent;
	private readonly FigureRecipe[] children;
	private readonly Reindexer reindexer;

	public FigureRecipeMerger(FigureRecipe parent, params FigureRecipe[] children) {
		this.parent = parent;
		this.children = children;
		this.reindexer = Reindexer.Make(parent, children);
	}
	
	private GeometryRecipe MergeGeometry() {
		return GeometryRecipe.Merge(
			reindexer,
			parent.Geometry,
			children.Select(child => child.Geometry).ToArray());
	}

	private List<ChannelRecipe> MergeChannels() {
		List<ChannelRecipe> mergedChannels = new List<ChannelRecipe>(parent.Channels);

		Dictionary<string, ChannelRecipe> parentChannelsByName = parent.Channels
			.ToDictionary(channel => channel.Name, channel => channel);

		foreach (var child in children) {
			foreach (var childChannel in child.Channels) {
				if (parentChannelsByName.TryGetValue(childChannel.Name, out var parentChannel)) {
					ChannelRecipe.VerifySimpleChild(parentChannel, childChannel);
				} else {
					mergedChannels.Add(childChannel);
				}
			}
		}
		
		return mergedChannels;
	}

	private List<FormulaRecipe> MergeFormulas() {
		foreach (var child in children) {
			if (child.Formulas.Count > 0) {
				throw new NotImplementedException();
			}
		}
		
		return parent.Formulas;
	}

	private List<BoneRecipe> MergeBones() {
		//children should already be sharing bones with their parents anyway
		return parent.Bones;
	}

	private List<MorphRecipe> MergeMorphs() {
		var parentMorphsByName = parent.Morphs.ToDictionary(morph => morph.Channel, morph => morph);
		var childMorphsByName = children.Select(child => child.Morphs.ToDictionary(morph => morph.Channel, morph => morph)).ToList();

		var allMorphNames = parentMorphsByName.Keys.Concat(childMorphsByName.SelectMany(dict => dict.Keys)).Distinct().ToList();

		var childAutomorphers = children.Select(child => child.Automorpher).ToArray();

		List<MorphRecipe> mergedMorphs = new List<MorphRecipe>();
		foreach (string morphName in allMorphNames) {
			parentMorphsByName.TryGetValue(morphName, out var parentMorph);

			var childMorphs = new MorphRecipe[childMorphsByName.Count];
			for (int childIdx = 0; childIdx < childMorphsByName.Count; ++childIdx) {
				childMorphsByName[childIdx].TryGetValue(morphName, out childMorphs[childIdx]);
			}
			
			var mergedMorph = MorphRecipe.Merge(morphName, reindexer, parentMorph, childMorphs, childAutomorphers);
			mergedMorphs.Add(mergedMorph);
		}

		return mergedMorphs;
	}

	private AutomorpherRecipe MergeAutomorpher() {
		if (parent.Automorpher != null) {
			throw new NotImplementedException();
		}

		return null;
	}

	public SkinBindingRecipe MergeSkinBinding() {
		return SkinBindingRecipe.Merge(
			reindexer,
			parent.SkinBinding,
			children.Select(child => child.SkinBinding).ToArray());
	}
	
	public List<UvSetRecipe> MergeUvSets() {
		UvSetRecipe[] childUvSets = children
			.Select(child => {
				if (child.UvSets.Count > 1) {
					throw new InvalidOperationException("expected child to have only a single UV set");
				}
				return child.UvSets[0];
			})
			.ToArray();

		return parent.UvSets
			.Select(parentUvSet => {
				return UvSetRecipe.Merge(reindexer, parentUvSet, childUvSets);
			})
			.ToList();

	}
	
	public FigureRecipe Merge() {
		GeometryRecipe mergedGeometry = MergeGeometry();
		List<ChannelRecipe> mergedChannels = MergeChannels();
		List<FormulaRecipe> mergedFormulas = MergeFormulas();
		List<BoneRecipe> mergedBones = MergeBones();
		List<MorphRecipe> mergedMorphs = MergeMorphs();
		AutomorpherRecipe mergedAutomorpher = MergeAutomorpher();
		SkinBindingRecipe mergedSkinBinding = MergeSkinBinding();
		List<UvSetRecipe> mergedUvSets = MergeUvSets();

		return new FigureRecipe {
			Geometry = mergedGeometry,
			Channels = mergedChannels,
			Formulas = mergedFormulas,
			Bones = mergedBones,
			Morphs = mergedMorphs,
			Automorpher = mergedAutomorpher,
			SkinBinding = mergedSkinBinding,
			UvSets = mergedUvSets
		};
	}
}
