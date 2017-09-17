#pragma once

#include "..\OpenSubdivFacadeNative\OpenSubdivFacadeNative.h"

namespace OpenSubdivFacade {
	public enum class StencilKind {
		LevelStencils,
		LimitStencils,
		LimitDuStencils,
		LimitDvStencils
	};

	public enum class VertexRule : int {
		Unknown = 0,
		Smooth = (1 << 0),
		Dart = (1 << 1),
		Crease = (1 << 2),
		Corner = (1 << 3)
	};

	public ref class Refinement
	{
	private:
		OpenSubdivFacadeNative::RefinerFacade* refiner;

	public:
		Refinement(QuadTopology^ controlTopology, int level) {
			pin_ptr<Quad> facesPinned = &controlTopology->Faces[0];

			refiner = OpenSubdivFacadeNative::MakeRefinerFacade(
				controlTopology->VertexCount,
				controlTopology->Faces->Length,
				(OpenSubdivFacadeNative::Quad*) facesPinned,
				level);
		}

		~Refinement() {
			delete refiner;
		}

		QuadTopology^ GetTopology() {
			int vertexCount = refiner->GetVertexCount();
			int faceCount = refiner->GetFaceCount();

			array<Quad>^ faces = gcnew array<Quad>(faceCount);
			pin_ptr<Quad> facesPinned = &faces[0];

			refiner->FillFaces((OpenSubdivFacadeNative::Quad*) facesPinned);

			return gcnew QuadTopology(vertexCount, faces);
		}

		array<int>^ GetFaceMap() {
			int faceCount = refiner->GetFaceCount();

			array<int>^ faceMap = gcnew array<int>(faceCount);
			pin_ptr<int> faceMapPinned = &faceMap[0];

			refiner->FillFaceMap(faceMapPinned);

			return faceMap;
		}

		PackedLists<int>^ GetAdjacentVertices() {
			int vertexCount = refiner->GetVertexCount();
			int edgeCount = refiner->GetEdgeCount();

			array<ArraySegment>^ segments = gcnew array<ArraySegment>(vertexCount);
			array<int>^ packedAdjacentVertices = gcnew array<int>(edgeCount * 2);
			pin_ptr<ArraySegment> segmentsPinned = &segments[0];
			pin_ptr<int> packedAdjacentVerticesPinned = &packedAdjacentVertices[0];
			refiner->FillAdjacentVertices(
				(OpenSubdivFacadeNative::ArraySegment*) segmentsPinned,
				(int*) packedAdjacentVerticesPinned);

			return gcnew PackedLists<int>(segments, packedAdjacentVertices);
		}

		array<VertexRule>^ GetVertexRules() {
			int vertexCount = refiner->GetVertexCount();

			array<VertexRule>^ rules = gcnew array<VertexRule>(vertexCount);
			pin_ptr<VertexRule> rulesPinned = &rules[0];
			refiner->FillVertexRules((int*) rulesPinned);

			return rules;
		}

		PackedLists<WeightedIndex>^ GetStencils(StencilKind kind) {
			OpenSubdivFacadeNative::StencilKind nativeKind = (OpenSubdivFacadeNative::StencilKind) kind;

			int vertexCount = refiner->GetVertexCount();
			int weightedIndexCount = refiner->GetStencilWeightCount(nativeKind);

			array<ArraySegment>^ segments = gcnew array<ArraySegment>(vertexCount);
			array<WeightedIndex>^ weightedIndices = gcnew array<WeightedIndex>(weightedIndexCount);
			pin_ptr<ArraySegment> segmentsPinned = &segments[0];
			pin_ptr<WeightedIndex> weightedIndicesPinned = &weightedIndices[0];
			refiner->FillStencils(
				nativeKind,
				(OpenSubdivFacadeNative::ArraySegment*) segmentsPinned,
				(OpenSubdivFacadeNative::WeightedIndex*) weightedIndicesPinned);

			return gcnew PackedLists<WeightedIndex>(segments, weightedIndices);
		}
	};
}
