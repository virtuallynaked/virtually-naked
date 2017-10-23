class RunImporterThenViewerApp : IDemoApp {
	public void Run() {
		ImporterMain.Main(new string[] {});
		VRApp.Main(new string[] { "--data=work" });
	}
}