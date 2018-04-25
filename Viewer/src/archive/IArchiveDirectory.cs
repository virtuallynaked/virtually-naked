using System.Collections.Generic;
using System.IO;

public interface IArchiveDirectory {
	string Name { get; }
	IArchiveDirectory Subdirectory(string name);
	IArchiveFile File(string name);
	IEnumerable<IArchiveFile> GetFiles();
	IEnumerable<IArchiveDirectory> Subdirectories { get; }
}

public static class IArchiveDirectoryExtensions {
	public static IArchiveFile File(this IArchiveDirectory dir, string[] path) {
		for (int i = 0; i < path.Length - 1; ++i) {
			dir = dir.Subdirectory(path[i]);
		}
		return dir.File(path[path.Length - 1]);
	}
}
