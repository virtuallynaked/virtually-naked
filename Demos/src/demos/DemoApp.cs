using System;

class DemoApp {
	[STAThread]
	public static void Main(string[] args) {
		//new PackArchiveDemo().Run();
		ImporterMain.Main(args);
		VRApp.Main(new string[] { "--data=work" });
		//new AnalyzeDazOutputApp().Run();
		//new AnalyzeDazColorSwatchesApp().Run();
		//new Mixamo.AnimationImporterDemo().Run();
		//new OpenSubdivFacadeDemo().Run();
		//new SampleCubeMapApp().Run();
		//new SpeechDemo().Run();
		//new WpfRenderingDemo().Run();
		//new ColorConversionDemo().Run();
		//new BumpToNormalDemo().Run();
		//new FaceTransparencyDemo().Run();
		//new FramePreparerDemo().Run();
		//new BoneSystemPerformanceDemo().Run();
	}
}

