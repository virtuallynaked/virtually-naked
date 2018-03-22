using System;
using System.Collections.Generic;
using System.Linq;

class SkinBindingImporter {
	private readonly List<SkinBindingRecipe> recipes = new List<SkinBindingRecipe>();

	public List<SkinBindingRecipe> Recipes => recipes;

	public void Import(DsonTypes.SkinBinding skinBinding) {
		int vertexCount = skinBinding.vertex_count;
		
		List<List<BoneWeight>> boneWeightsByVertex = new List<List<BoneWeight>>(vertexCount);
		for (int i = 0; i < vertexCount; ++i) {
			boneWeightsByVertex.Add(new List<BoneWeight>());
		}

		DsonTypes.WeightedJoint[] joints = skinBinding.joints;

		string[] boneNames = new string[joints.Length];
		
		for (int boneIdx = 0; boneIdx < joints.Length; ++boneIdx) {
			DsonTypes.WeightedJoint joint = joints[boneIdx];
			
			if (joint.node_weights == null) {
				throw new InvalidOperationException("expected scale_weights to be non-null");
			}
			if (joint.scale_weights != null) {
				throw new InvalidOperationException("expected scale_weights to be null");
			}
			if (joint.local_weights != null) {
				throw new InvalidOperationException("expected local_weights to be null");
			}
			if (joint.bulge_weights != null) {
				throw new InvalidOperationException("expected bulge_weights to be null");
			}

			DsonTypes.Node jointNode = joint.node.ReferencedObject;
			boneNames[boneIdx] = jointNode.name;

			foreach (DsonTypes.IndexedFloat elem in joint.node_weights.values) {
				boneWeightsByVertex[elem.index].Add(new BoneWeight(boneIdx, (float) elem.value));
			}
		}
		
		Dictionary<string, string> faceGroupToNodeMap = new Dictionary<string, string>();
		if (skinBinding.selection_map.Length != 1) {
			throw new InvalidOperationException("expected only one face-group-to-node map");
		}
		foreach (DsonTypes.StringPair pair in skinBinding.selection_map[0].mappings) {
			faceGroupToNodeMap.Add(pair.from, pair.to);
		}

		SkinBindingRecipe recipe = new SkinBindingRecipe {
			BoneNames = boneNames,
			BoneWeights = PackedLists<BoneWeight>.Pack(boneWeightsByVertex),
			FaceGroupToNodeMap = faceGroupToNodeMap
		};
		recipes.Add(recipe);
	}

	public void ImportFrom(DsonTypes.Modifier modifier) {
		if (modifier.skin != null) {
			Import(modifier.skin);
		}
	}

	public void ImportFrom(DsonTypes.Modifier[] modifiers) {
		if (modifiers == null) {
			return;
		}

		foreach (var modifier in modifiers) {
			ImportFrom(modifier);
		}
	}

	public void ImportFrom(DsonTypes.DsonDocument doc) {
		ImportFrom(doc.Root.modifier_library);
	}

	public static SkinBindingRecipe ImportForFigure(DsonObjectLocator locator, FigureUris figureUris) {
		SkinBindingImporter importer = new SkinBindingImporter();

		importer.ImportFrom(locator.LocateRoot(figureUris.DocumentUri));

		return importer.recipes.Single();
	}
}
