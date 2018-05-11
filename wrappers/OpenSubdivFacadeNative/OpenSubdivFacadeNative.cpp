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
	static const int QuadVertexCount = 4;

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

		const Far::TopologyLevel& GetTopology(int level) {
			return refiner->GetLevel(level);
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

			std::vector<int> numVertsPerFace;
			std::vector<int> vertIndicesPerFace;
			numVertsPerFace.reserve(faceCount);
			vertIndicesPerFace.reserve(faceCount * QuadVertexCount);

			for (int faceIdx = 0; faceIdx < faceCount; ++faceIdx) {
				Quad face = faces[faceIdx];
				if (face.index2 == face.index3) {
					//this is actually a triangle
					numVertsPerFace.push_back(QuadVertexCount - 1);
					vertIndicesPerFace.push_back(face.index0);
					vertIndicesPerFace.push_back(face.index1);
					vertIndicesPerFace.push_back(face.index2);
				}
				else {
					//truly a quad
					numVertsPerFace.push_back(QuadVertexCount);
					vertIndicesPerFace.push_back(face.index0);
					vertIndicesPerFace.push_back(face.index1);
					vertIndicesPerFace.push_back(face.index2);
					vertIndicesPerFace.push_back(face.index3);
				}
			}

			Far::TopologyDescriptor topologyDescriptor;
			topologyDescriptor.numVertices = vertexCount;
			topologyDescriptor.numFaces = faceCount;
			topologyDescriptor.numVertsPerFace = &numVertsPerFace[0];
			topologyDescriptor.vertIndicesPerFace = &vertIndicesPerFace[0];
			topologyDescriptor.numFVarChannels = 0;

			refiner = std::unique_ptr<Far::TopologyRefiner>(Far::TopologyRefinerFactory<Far::TopologyDescriptor>::Create(
				topologyDescriptor,
				Far::TopologyRefinerFactory<Far::TopologyDescriptor>::Options(type, options)));

			Far::TopologyRefiner::UniformOptions refineOptions(refinementLevel);
			refineOptions.fullTopologyInLastLevel = true;
			refiner->RefineUniform(refineOptions);

			primvarRefiner = std::make_unique<Far::PrimvarRefiner>(*refiner);
		}

		int GetFaceCount(int level) {
			return GetTopology(level).GetNumFaces();
		}
		
		int GetVertexCount(int level) {
			return GetTopology(level).GetNumVertices();
		}

		int GetEdgeCount(int level) {
			return GetTopology(level).GetNumEdges();
		}

		void FillFaces(int level, Quad* quads) {
			const Far::TopologyLevel& topology = GetTopology(level);

			int count = topology.GetNumFaces();
			for (int faceIdx = 0; faceIdx < count; ++faceIdx) {
				auto face = topology.GetFaceVertices(faceIdx);
				int vertexCount = face.size();
				if (vertexCount == QuadVertexCount) {
					quads[faceIdx].index0 = face[0];
					quads[faceIdx].index1 = face[1];
					quads[faceIdx].index2 = face[2];
					quads[faceIdx].index3 = face[3];
				}
				else if (vertexCount == QuadVertexCount - 1) {
					quads[faceIdx].index0 = face[0];
					quads[faceIdx].index1 = face[1];
					quads[faceIdx].index2 = face[2];
					quads[faceIdx].index3 = face[2]; //duplicate the last vertex to signal this is a triangle
				}
				else {
					throw new std::exception("invalid vertex count: " + vertexCount);
				}
			}
		}

		void FillFaceMap(int* faceMap) {
			int maxLevel = refiner->GetMaxLevel();
			const Far::TopologyLevel& topology = GetTopology(maxLevel);

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
			int maxLevel = refiner->GetMaxLevel();
			const Far::TopologyLevel& topology = GetTopology(maxLevel);

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
			int maxLevel = refiner->GetMaxLevel();
			const Far::TopologyLevel& topology = GetTopology(maxLevel);

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

		void FillRefinedValues(int level, Vector3* previousLevelValues, Vector3* refinedValues) {
			primvarRefiner->Interpolate(level, previousLevelValues, refinedValues);
		}

		void FillRefinedValues(int level, Vector2* previousLevelValues, Vector2* refinedValues) {
			primvarRefiner->Interpolate(level, previousLevelValues, refinedValues);
		}
	};

	RefinerFacade* MakeRefinerFacade(int vertexCount, int faceCount, const Quad* faces, int refinementLevel) {
#ifdef _DEBUG
		fprintf(stderr, "%s\n", "WARNING: Using OpenSubdiv Debug build");
#endif
		return new RefinerFacadeImpl(vertexCount, faceCount, faces, refinementLevel);
	}
}
