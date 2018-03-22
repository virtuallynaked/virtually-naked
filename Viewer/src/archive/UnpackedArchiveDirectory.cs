using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class UnpackedArchiveDirectory : IArchiveDirectory {
	public static UnpackedArchiveDirectory Make(DirectoryInfo info) {
		return info.Exists ? new UnpackedArchiveDirectory(info) : null;
	}

	private readonly DirectoryInfo info;
	
	private UnpackedArchiveDirectory(DirectoryInfo info) {
		this.info = info;
	}

	public string Name => info.Name;
		
	public IArchiveDirectory Subdirectory(string name) {
		return Make(info.Subdirectory(name));
	}
	
	public IArchiveFile File(string name) {
		return UnpackedArchiveFile.Make(info.File(name));
	}

	public IEnumerable<IArchiveFile> GetFiles() {
		return info.GetFiles().Select(fileInfo => UnpackedArchiveFile.Make(fileInfo));
	}
	
	public IEnumerable<IArchiveDirectory> Subdirectories => info.GetDirectories().Select(dirInfo => UnpackedArchiveDirectory.Make(dirInfo));
}
