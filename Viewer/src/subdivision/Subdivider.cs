using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

public class Subdivider {
    private readonly PackedLists<WeightedIndex> stencils;

    public Subdivider(PackedLists<WeightedIndex> stencils) {
        this.stencils = stencils;
    }

    public T[] Refine<T, U>(T[] controlVectors, U operators) where U : IVectorOperators<T> {
        T[] refinedVertices = new T[stencils.Count];

        for (int i = 0; i < stencils.Count; ++i) {
			T refinedVertex = operators.Zero();
            foreach (var weightedIndex in stencils.GetElements(i)) {
				refinedVertex = operators.Add(refinedVertex, operators.Mul(weightedIndex.Weight, controlVectors[weightedIndex.Index]));
            }

            refinedVertices[i] = refinedVertex;
        }

        return refinedVertices;
    }

    public Vector3[] Unrefine(Vector3[] refinedVertices) {
        if (refinedVertices.Length != stencils.Count) {
            throw new InvalidOperationException("refined vertex count must match stencil count");
        }

        Vector3[] controlVertices = (Vector3[]) refinedVertices.Clone();
        
        int iterationCount = 0;
        while (true) {
            float maxDiff = 0;

            for (int i = 0; i < stencils.Count; ++i) {
                float diagonalWeight = 0;
                Vector3 offDiagonalWeightedSum = Vector3.Zero;

                foreach (var weightedIndex in stencils.GetElements(i)) {
                    if (weightedIndex.Index == i) {
                        diagonalWeight = weightedIndex.Weight;
                    } else {
                        offDiagonalWeightedSum += weightedIndex.Weight * controlVertices[weightedIndex.Index];
                    }
                }

                if (diagonalWeight == 0) {
                    throw new InvalidOperationException("diagonal weight must be non-zero");
                }

                Vector3 error = (refinedVertices[i] - offDiagonalWeightedSum - diagonalWeight * controlVertices[i]);
                controlVertices[i] = (refinedVertices[i] - offDiagonalWeightedSum) / diagonalWeight;
                
                for (int dim = 0; dim < 3; ++dim) {
                    float absDiff = Math.Abs(error[dim]);
                    if (absDiff > maxDiff)
                        maxDiff = absDiff;
                }
            }
                        
            if (maxDiff < 1e-4) {
                break;
            }

            Debug.WriteLine(iterationCount + ": " + maxDiff);

            iterationCount += 1;
            if (iterationCount >= 1000) {
                throw new InvalidOperationException("too many iterations");
            }
        }

        return controlVertices;
    }
}
