using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Linq;

public class FaceTransparencyDemo : IDemoApp {
	private readonly ContentFileLocator fileLocator;
	private readonly DsonObjectLocator objectLocator;
	private readonly Device device;
	private readonly ShaderCache shaderCache;

	public FaceTransparencyDemo() {
		fileLocator = new ContentFileLocator();
		objectLocator = new DsonObjectLocator(fileLocator);
		device = new Device(DriverType.Hardware, DeviceCreationFlags.None, FeatureLevel.Level_11_1);
		shaderCache = new ShaderCache(device);
	}

	public void Run() {
		var loader = new FigureRecipeLoader(objectLocator);

		FigureRecipe genesis3FemaleRecipe = loader.LoadFigureRecipe("genesis-3-female", null);
		FigureRecipe genitaliaRecipe = loader.LoadFigureRecipe("genesis-3-female-genitalia", genesis3FemaleRecipe);
		FigureRecipe genesis3FemaleWithGenitaliaRecipe = new FigureRecipeMerger(genesis3FemaleRecipe, genitaliaRecipe).Merge();
		Figure genesis3FemaleWithGenitalia = genesis3FemaleWithGenitaliaRecipe.Bake("genesis-3-female", null);

		Figure parentFigure = genesis3FemaleWithGenitalia;

		FigureRecipe livHairRecipe = loader.LoadFigureRecipe("liv-hair", null);
		var livHairFigure = livHairRecipe.Bake("liv-hair", parentFigure);

		var faceTransparencyCalculator = new FromTextureFaceTransparencyCalculator(fileLocator, device, shaderCache, livHairFigure);
		
		var transparencies = faceTransparencyCalculator.CalculateSurfaceTransparencies();
		for (int i = 0; i < 10; ++i) {
			int faceIdx = i * 3000;
			int surfaceIdx  = livHairFigure.Geometry.SurfaceMap[faceIdx];
			string surfaceName = livHairFigure.Geometry.SurfaceNames[surfaceIdx];

			var uvSet = livHairFigure.DefaultUvSet;
			Quad face = uvSet.Faces[faceIdx];
			
			Console.WriteLine("face {0}: ", faceIdx);
			Console.WriteLine("  transparency: " + transparencies[faceIdx]);
			Console.WriteLine("  surface: " + surfaceName);
			Console.WriteLine("  uv 0: {0}", uvSet.Uvs[face.Index0] );
			Console.WriteLine("  uv 1: {0}", uvSet.Uvs[face.Index1] );
			Console.WriteLine("  uv 2: {0}", uvSet.Uvs[face.Index2] );
			Console.WriteLine("  uv 3: {0}", uvSet.Uvs[face.Index3] );
			Console.WriteLine();
		}
		Console.WriteLine("min = " + transparencies.Min());
		Console.WriteLine("avg = " + transparencies.Average());
		Console.WriteLine("max = " + transparencies.Max());
	}
}
