using System.Collections.Generic;
using System.IO;
using System.Linq;

public class UnpackedArchiveDirectory : IArchiveDirectory {
	public static UnpackedArchiveDirectory Make(DirectoryInfo info) {
		return info.Exists ? new UnpackedArchiveDirectory(info) : null;
	}

	private readonly DirectoryInfo info;
	private readonly Dictionary<string, UnpackedArchiveDirectory> subdirectories;
	private readonly Dictionary<string, UnpackedArchiveFile> files;
	
	private UnpackedArchiveDirectory(DirectoryInfo info) {
		this.info = info;

		subdirectories = info.GetDirectories().ToDictionary(
			subdirInfo => subdirInfo.Name,
			subdirInfo => Make(subdirInfo));

		files = info.GetFiles().ToDictionary(
			fileInfo => fileInfo.Name,
			fileInfo => UnpackedArchiveFile.Make(fileInfo));
	}

	public string Name => info.Name;
		
	public IArchiveFile File(string name) {
		files.TryGetValue(name, out var file);
		return file;
	}

	public IEnumerable<IArchiveFile> GetFiles() {
		return files.Values;
	}

	public IEnumerable<IArchiveDirectory> Subdirectories => subdirectories.Values;

	public IArchiveDirectory Subdirectory(string name) {
		subdirectories.TryGetValue(name, out var subdir);
		return subdir;
	}
}
