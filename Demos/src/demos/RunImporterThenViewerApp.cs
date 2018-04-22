class RunImporterThenViewerApp : IDemoApp {
	public void Run() {
		ImporterMain.Main(new string[] {});
		VRApp.Main(new string[] { "--content=work\\content" });
	}
}
