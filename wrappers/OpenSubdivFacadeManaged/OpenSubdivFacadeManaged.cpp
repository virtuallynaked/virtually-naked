#pragma once

#include "..\OpenSubdivFacadeNative\OpenSubdivFacadeNative.h"

namespace OpenSubdivFacade {
	public enum class BoundaryInterpolation {
		None,
		EdgeOnly,
		EdgeAndCorner
	};

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

	generic<typename T>
	public value struct LimitValues {
		array<T>^ values;
		array<T>^ tangents1;
		array<T>^ tangents2;
	};

	public ref class Refinement
	{
	private:
		int maxLevel;
		OpenSubdivFacadeNative::RefinerFacade* refiner;

	public:
		Refinement(QuadTopology^ controlTopology, int level) : Refinement(controlTopology, level, BoundaryInterpolation::EdgeOnly) {
		}

		Refinement(QuadTopology^ controlTopology, int level, BoundaryInterpolation boundaryInterpolation) {
			maxLevel = level;

			pin_ptr<Quad> facesPinned = &controlTopology->Faces[0];

			refiner = OpenSubdivFacadeNative::MakeRefinerFacade(
				controlTopology->VertexCount,
				controlTopology->Faces->Length,
				(OpenSubdivFacadeNative::Quad*) facesPinned,
				level,
				(OpenSubdivFacadeNative::BoundaryInterpolation) boundaryInterpolation);
		}

		~Refinement() {
			delete refiner;
		}

		QuadTopology^ GetTopology() {
			return GetTopology(maxLevel);
		}

		QuadTopology^ GetTopology(int level) {
			int vertexCount = refiner->GetVertexCount(level);
			int faceCount = refiner->GetFaceCount(level);

			array<Quad>^ faces = gcnew array<Quad>(faceCount);
			pin_ptr<Quad> facesPinned = &faces[0];

			refiner->FillFaces(level, (OpenSubdivFacadeNative::Quad*) facesPinned);

			return gcnew QuadTopology(vertexCount, faces);
		}

		array<int>^ GetFaceMap() {
			int faceCount = refiner->GetFaceCount(maxLevel);

			array<int>^ faceMap = gcnew array<int>(faceCount);
			pin_ptr<int> faceMapPinned = &faceMap[0];

			refiner->FillFaceMap(faceMapPinned);

			return faceMap;
		}
		
		PackedLists<WeightedIndex>^ GetStencils(StencilKind kind) {
			OpenSubdivFacadeNative::StencilKind nativeKind = (OpenSubdivFacadeNative::StencilKind) kind;

			int vertexCount = refiner->GetVertexCount(maxLevel);
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

		/*
		 * Refine values from (level - 1) to level.
		 */
		array<SharpDX::Vector3>^ Refine(int level, array<SharpDX::Vector3>^ previousLevelValues) {
			int refineVertexCount = refiner->GetVertexCount(level);
			array<SharpDX::Vector3>^ refinedValues = gcnew array<SharpDX::Vector3>(refineVertexCount);

			pin_ptr<SharpDX::Vector3> previousLevelValuesPinned = &previousLevelValues[0];
			pin_ptr<SharpDX::Vector3> refinedValuesPinned = &refinedValues[0];
			refiner->FillRefinedValues(
				level,
				(OpenSubdivFacadeNative::Vector3*) previousLevelValuesPinned,
				(OpenSubdivFacadeNative::Vector3*) refinedValuesPinned);

			return refinedValues;
		}

		/*
		* Refine values from (level - 1) to level.
		*/
		array<SharpDX::Vector2>^ Refine(int level, array<SharpDX::Vector2>^ previousLevelValues) {
			int refineVertexCount = refiner->GetVertexCount(level);
			array<SharpDX::Vector2>^ refinedValues = gcnew array<SharpDX::Vector2>(refineVertexCount);

			pin_ptr<SharpDX::Vector2> previousLevelValuesPinned = &previousLevelValues[0];
			pin_ptr<SharpDX::Vector2> refinedValuesPinned = &refinedValues[0];
			refiner->FillRefinedValues(
				level,
				(OpenSubdivFacadeNative::Vector2*) previousLevelValuesPinned,
				(OpenSubdivFacadeNative::Vector2*) refinedValuesPinned);

			return refinedValues;
		}

		/*
		 * Refine values from maxLevel to limit surface (including tangents).
		 */
		LimitValues<SharpDX::Vector3> Limit(array<SharpDX::Vector3>^ maxLevelValues) {
			int vertexCount = maxLevelValues->Length;
			array<SharpDX::Vector3>^ limitValues = gcnew array<SharpDX::Vector3>(vertexCount);
			array<SharpDX::Vector3>^ tan1Values = gcnew array<SharpDX::Vector3>(vertexCount);
			array<SharpDX::Vector3>^ tan2Values = gcnew array<SharpDX::Vector3>(vertexCount);

			pin_ptr<SharpDX::Vector3> maxLevelValuesPinned = &maxLevelValues[0];
			pin_ptr<SharpDX::Vector3> limitValuesPinned = &limitValues[0];
			pin_ptr<SharpDX::Vector3> tan1ValuesPinned = &tan1Values[0];
			pin_ptr<SharpDX::Vector3> tan2ValuesPinned = &tan2Values[0];
			refiner->FillLimitValues(
				(OpenSubdivFacadeNative::Vector3*) maxLevelValuesPinned,
				(OpenSubdivFacadeNative::Vector3*) limitValuesPinned,
				(OpenSubdivFacadeNative::Vector3*) tan1ValuesPinned,
				(OpenSubdivFacadeNative::Vector3*) tan2ValuesPinned);

			LimitValues<SharpDX::Vector3> result;
			result.values = limitValues;
			result.tangents1 = tan1Values;
			result.tangents2 = tan2Values;

			return result;
		}

		/*
		 * Refine values from maxLevel to limit surface (including tangents).
		 */
		LimitValues<SharpDX::Vector2> Limit(array<SharpDX::Vector2>^ maxLevelValues) {
			int vertexCount = maxLevelValues->Length;
			array<SharpDX::Vector2>^ limitValues = gcnew array<SharpDX::Vector2>(vertexCount);
			array<SharpDX::Vector2>^ tan1Values = gcnew array<SharpDX::Vector2>(vertexCount);
			array<SharpDX::Vector2>^ tan2Values = gcnew array<SharpDX::Vector2>(vertexCount);

			pin_ptr<SharpDX::Vector2> maxLevelValuesPinned = &maxLevelValues[0];
			pin_ptr<SharpDX::Vector2> limitValuesPinned = &limitValues[0];
			pin_ptr<SharpDX::Vector2> tan1ValuesPinned = &tan1Values[0];
			pin_ptr<SharpDX::Vector2> tan2ValuesPinned = &tan2Values[0];
			refiner->FillLimitValues(
				(OpenSubdivFacadeNative::Vector2*) maxLevelValuesPinned,
				(OpenSubdivFacadeNative::Vector2*) limitValuesPinned,
				(OpenSubdivFacadeNative::Vector2*) tan1ValuesPinned,
				(OpenSubdivFacadeNative::Vector2*) tan2ValuesPinned);

			LimitValues<SharpDX::Vector2> result;
			result.values = limitValues;
			result.tangents1 = tan1Values;
			result.tangents2 = tan2Values;

			return result;
		}

		array<SharpDX::Vector3>^ RefineFully(array<SharpDX::Vector3>^ controlValues) {
			array<SharpDX::Vector3>^ values = controlValues;
			for (int level = 1; level <= maxLevel; ++level) {
				values = Refine(level, values);
			}
			return values;
		}

		array<SharpDX::Vector2>^ RefineFully(array<SharpDX::Vector2>^ controlValues) {
			array<SharpDX::Vector2>^ values = controlValues;
			for (int level = 1; level <= maxLevel; ++level) {
				values = Refine(level, values);
			}
			return values;
		}

		LimitValues<SharpDX::Vector3> LimitFully(array<SharpDX::Vector3>^ controlValues) {
			array<SharpDX::Vector3>^ refinedValues = RefineFully(controlValues);
			return Limit(refinedValues);
		}

		LimitValues<SharpDX::Vector2> LimitFully(array<SharpDX::Vector2>^ controlValues) {
			array<SharpDX::Vector2>^ refinedValues = RefineFully(controlValues);
			return Limit(refinedValues);
		}
	};
}
