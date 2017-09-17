#pragma once

namespace OpenSubdivFacadeNative {
	enum StencilKind {
		LevelStencils,
		LimitStencils,
		LimitDuStencils,
		LimitDvStencils
	};

	struct Quad {
		int index0;
		int index1;
		int index2;
		int index3;
	};

	struct Topology {
		int vertexCount;
		int faceCount;
	};

	struct ArraySegment {
		int offset;
		int count;
	};

	struct WeightedIndex {
		int index;
		float weight;
	};

	class RefinerFacade {
	public:
		virtual int GetFaceCount() = 0;
		virtual int GetVertexCount() = 0;
		virtual int GetEdgeCount() = 0;
		virtual void FillFaces(Quad* quads) = 0;
		virtual void FillFaceMap(int* faceMap) = 0;
		virtual void FillAdjacentVertices(ArraySegment* segments, int* adjacentVertices) = 0;
		virtual void FillVertexRules(int* rules) = 0;
		virtual int GetStencilWeightCount(StencilKind kind) = 0;
		virtual void FillStencils(StencilKind kind, ArraySegment* segments, WeightedIndex* weights) = 0;
	};

	RefinerFacade* MakeRefinerFacade(int vertexCount, int faceCount, const Quad* faces, int refinementLevel);
}
