public class PackContentApp : IDemoApp {
	public void Run() {
		var contentDir = CommonPaths.WorkDir.Subdirectory("content");

		var packedContentDir = CommonPaths.WorkDir.Subdirectory("packed-content");
		packedContentDir.CreateWithParents();

		foreach (var subDir in contentDir.GetDirectories()) {
			var packer = new ArchivePacker();
			packer.Pack(packedContentDir.File($"{subDir.Name}.archive"), subDir);
		}
	}
}
