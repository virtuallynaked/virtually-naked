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

	struct Vector3 {
		float x;
		float y;
		float z;

		void Clear() {
			x = 0;
			y = 0;
			z = 0;
		}

		void AddWithWeight(const Vector3& accumulator, float weight) {
			x += weight * accumulator.x;
			y += weight * accumulator.y;
			z += weight * accumulator.z;
		}
	};

	class RefinerFacade {
	public:
		virtual int GetFaceCount(int level) = 0;
		virtual int GetVertexCount(int level) = 0;
		virtual int GetEdgeCount(int level) = 0;
		virtual void FillFaces(int level, Quad* quads) = 0;
		virtual void FillFaceMap(int* faceMap) = 0;
		virtual void FillAdjacentVertices(ArraySegment* segments, int* adjacentVertices) = 0;
		virtual void FillVertexRules(int* rules) = 0;
		virtual int GetStencilWeightCount(StencilKind kind) = 0;
		virtual void FillStencils(StencilKind kind, ArraySegment* segments, WeightedIndex* weights) = 0;
		virtual void FillRefinedValues(int level, Vector3* previousLevelValues, Vector3* refinedValues) = 0;
	};

	RefinerFacade* MakeRefinerFacade(int vertexCount, int faceCount, const Quad* faces, int refinementLevel);
}
