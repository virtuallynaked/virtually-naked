#include "OpenSubdivFacadeNative.h"

#include <opensubdiv/far/topologyDescriptor.h>
#include <opensubdiv/far/primvarRefiner.h>
#include <opensubdiv/far/stencilTableFactory.h>
#include <opensubdiv/osd/hlslPatchShaderSource.h>
#include <opensubdiv/osd/cpuEvaluator.h>
#include <opensubdiv/osd/cpuVertexBuffer.h>

#include <memory>
#include <unordered_map>
#include <cstdio>

using namespace OpenSubdiv;

namespace OpenSubdivFacadeNative {
	class Stencil {
	public:
		std::unordered_map<int, float> indexWeights;

		void Init(int controlIndex) {
			indexWeights[controlIndex] = 1;
		}

		void Clear() {
			indexWeights.clear();
		}

		void AddWithWeight(const Stencil& accumulator, float weight) {
			for (auto& it : accumulator.indexWeights) {
				indexWeights[it.first] += weight * it.second;
			}
		}
	};

	class RefinerFacadeImpl : public RefinerFacade {
		std::unique_ptr<Far::TopologyRefiner> refiner;
		std::unique_ptr<Far::PrimvarRefiner> primvarRefiner;

		std::vector<Stencil> levelStencils;
		std::vector<Stencil> limitStencils;
		std::vector<Stencil> limitDuStencils;
		std::vector<Stencil> limitDvStencils;

		void EnsureLevelStencils() {
			if (!levelStencils.empty()) {
				return;
			}

			//prepare bottom level
			int controlVertexCount = refiner->GetLevel(0).GetNumVertices();
			levelStencils.resize(controlVertexCount);
			for (int i = 0; i < controlVertexCount; ++i) {
				levelStencils[i].Init(i);
			}

			for (int levelIdx = 1; levelIdx <= refiner->GetMaxLevel(); ++levelIdx) {
				std::vector<Stencil> previousLevelStencils = std::move(levelStencils);

				int vertexCount = refiner->GetLevel(levelIdx).GetNumVertices();
				levelStencils.resize(vertexCount);

				primvarRefiner->Interpolate(levelIdx, previousLevelStencils, levelStencils);
			}
		}

		void EnsureLimitStencils() {
			EnsureLevelStencils();

			limitStencils.resize(levelStencils.size());
			limitDuStencils.resize(levelStencils.size());
			limitDvStencils.resize(levelStencils.size());
			primvarRefiner->Limit(levelStencils, limitStencils, limitDuStencils, limitDvStencils);
		}

		const Far::TopologyLevel& GetRefinedTopology() {
			int maxLevel = refiner->GetMaxLevel();
			return refiner->GetLevel(maxLevel);
		}

		const std::vector<Stencil>& GetStencils(StencilKind kind) {
			if (kind == LevelStencils) {
				EnsureLevelStencils();
				return levelStencils;
			}
			else if (kind == LimitStencils) {
				EnsureLimitStencils();
				return limitStencils;
			}
			else if (kind == LimitDuStencils) {
				EnsureLimitStencils();
				return limitDuStencils;
			}
			else if (kind == LimitDvStencils) {
				EnsureLimitStencils();
				return limitDvStencils;
			}
			else {
				throw new std::exception("invalid kind");
			}
		}

	public:
		RefinerFacadeImpl(int vertexCount, int faceCount, const Quad* faces, int refinementLevel) {
			Sdc::SchemeType type = OpenSubdiv::Sdc::SCHEME_CATMARK;

			Sdc::Options options;
			options.SetVtxBoundaryInterpolation(Sdc::Options::VTX_BOUNDARY_EDGE_ONLY);
			options.SetFVarLinearInterpolation(Sdc::Options::FVAR_LINEAR_NONE);

			std::vector<int> vertsPerFace(faceCount, 4);

			Far::TopologyDescriptor topologyDescriptor;
			topologyDescriptor.numVertices = vertexCount;
			topologyDescriptor.numFaces = faceCount;
			topologyDescriptor.numVertsPerFace = &vertsPerFace[0];
			topologyDescriptor.vertIndicesPerFace = (int*)faces;
			topologyDescriptor.numFVarChannels = 0;

			refiner = std::unique_ptr<Far::TopologyRefiner>(Far::TopologyRefinerFactory<Far::TopologyDescriptor>::Create(
				topologyDescriptor,
				Far::TopologyRefinerFactory<Far::TopologyDescriptor>::Options(type, options)));

			Far::TopologyRefiner::UniformOptions refineOptions(refinementLevel);
			refineOptions.fullTopologyInLastLevel = true;
			refiner->RefineUniform(refineOptions);

			primvarRefiner = std::make_unique<Far::PrimvarRefiner>(*refiner);
		}

		int GetFaceCount() {
			return GetRefinedTopology().GetNumFaces();
		}

		int GetVertexCount() {
			return GetRefinedTopology().GetNumVertices();
		}

		int GetEdgeCount() {
			return GetRefinedTopology().GetNumEdges();
		}

		void FillFaces(Quad* quads) {
			const Far::TopologyLevel& topology = GetRefinedTopology();

			int count = topology.GetNumFaces();
			for (int faceIdx = 0; faceIdx < count; ++faceIdx) {
				auto face = topology.GetFaceVertices(faceIdx);
				quads[faceIdx].index0 = face[0];
				quads[faceIdx].index1 = face[1];
				quads[faceIdx].index2 = face[2];
				quads[faceIdx].index3 = face[3];
			}
		}

		void FillFaceMap(int* faceMap) {
			int maxLevel = refiner->GetMaxLevel();
			const Far::TopologyLevel& topology = GetRefinedTopology();

			int count = topology.GetNumFaces();
			for (int faceIdx = 0; faceIdx < count; ++faceIdx) {
				int parentFaceIdx = faceIdx;
				for (int level = maxLevel; level > 0; level -= 1) {
					parentFaceIdx = refiner->GetLevel(level).GetFaceParentFace(parentFaceIdx);
				}
				faceMap[faceIdx] = parentFaceIdx;
			}
		}

		void FillAdjacentVertices(ArraySegment* segments, int* packedAdjacentVertices) {
			const Far::TopologyLevel& topology = GetRefinedTopology();

			int segmentIdx = 0;
			int packedAdjacentVerticesIdx = 0;

			int vertexCount = topology.GetNumVertices();
			for (int vertexIdx = 0; vertexIdx < vertexCount; ++vertexIdx) {
				const auto& adjacentEdgeIndices = topology.GetVertexEdges(vertexIdx);
				int adjacentEdgeCount = (int) adjacentEdgeIndices.size();

				segments[segmentIdx].offset = packedAdjacentVerticesIdx;
				segments[segmentIdx].count = adjacentEdgeCount;
				segmentIdx += 1;

				for (int i = 0; i < adjacentEdgeCount; ++i) {
					const auto& edgeVertices = topology.GetEdgeVertices(adjacentEdgeIndices[i]);
					int adjacentVertex = edgeVertices[0] == vertexIdx ? edgeVertices[1] : edgeVertices[0];

					packedAdjacentVertices[packedAdjacentVerticesIdx] = adjacentVertex;
					packedAdjacentVerticesIdx += 1;
				}
			}
		}

		void FillVertexRules(int* rules) {
			const Far::TopologyLevel& topology = GetRefinedTopology();

			int vertexCount = topology.GetNumVertices();
			for (int vertexIdx = 0; vertexIdx < vertexCount; ++vertexIdx) {
				rules[vertexIdx] = topology.GetVertexRule(vertexIdx);
			}
		}

		int GetStencilWeightCount(StencilKind kind) {
			const std::vector<Stencil>& stencils = GetStencils(kind);

			int count = 0;
			for (const auto& stencil : stencils) {
				count += (int)stencil.indexWeights.size();
			}
			return count;
		}

		void FillStencils(StencilKind kind, ArraySegment* segments, WeightedIndex* weights) {
			const std::vector<Stencil>& stencils = GetStencils(kind);

			int weightIdx = 0;
			int stencilIdx = 0;

			for (const auto& stencil : stencils) {
				segments[stencilIdx].offset = weightIdx;
				segments[stencilIdx].count = (int)stencil.indexWeights.size();
				stencilIdx += 1;

				for (auto it : stencil.indexWeights) {
					weights[weightIdx].index = it.first;
					weights[weightIdx].weight = it.second;
					weightIdx += 1;
				}
			}
		}
	};

	RefinerFacade* MakeRefinerFacade(int vertexCount, int faceCount, const Quad* faces, int refinementLevel) {
#ifdef _DEBUG
		fprintf(stderr, "%s\n", "WARNING: Using OpenSubdiv Debug build");
#endif
		return new RefinerFacadeImpl(vertexCount, faceCount, faces, refinementLevel);
	}
}

