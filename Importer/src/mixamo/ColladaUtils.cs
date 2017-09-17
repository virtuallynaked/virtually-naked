using SharpDX;
using System;
using System.Linq;

namespace Mixamo {
	public static class ColladaUtils {
		public static Matrix[] MatricesFromString(string str) {
			float[] values = str.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries)
					.Select(s => float.Parse(s))
					.ToArray();

			if (values.Length % 16 != 0) {
				throw new InvalidOperationException();
			}

			int matrixCount = values.Length / 16;
			Matrix[] matrices = new Matrix[matrixCount];
			for (int i = 0; i < matrixCount; ++i) {
				for (int j = 0; j < 16; ++j) {
					matrices[i][j] = values[i * 16 + j];
				}
				matrices[i].Transpose(); //post-multiply transforms instead of pre-multiply
			}

			return matrices;
		}

		internal static Matrix MatrixFromString(string str) {
			Matrix[] matrices = MatricesFromString(str);
			if (matrices.Length != 1) {
				throw new InvalidOperationException("expected a single matrix");
			}
			return matrices[0];
		}
	}
}


