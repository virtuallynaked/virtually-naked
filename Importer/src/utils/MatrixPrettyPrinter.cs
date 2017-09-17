using SharpDX;
using System;

static class MatrixPrettyPrinter {
	public static void PrintMatrix(String name, Matrix mat) {
		Console.WriteLine(name + ":");
		Console.WriteLine("\t" + mat.Row1);
		Console.WriteLine("\t" + mat.Row2);
		Console.WriteLine("\t" + mat.Row3);
		Console.WriteLine("\t" + mat.Row4);
		Console.WriteLine();
	}
}
