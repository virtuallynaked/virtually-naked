using System.Collections.Generic;
using System.Linq;

public class PackedArchiveDirectory : IArchiveDirectory {
	private readonly PackedArchive archive;
	private readonly PackedArchiveDirectoryRecord record;

	private readonly Dictionary<string, PackedArchiveDirectory> subdirectories;
	private readonly Dictionary<string, PackedArchiveFile> files;

	public PackedArchiveDirectory(PackedArchive archive, PackedArchiveDirectoryRecord record) {
		this.archive = archive;
		this.record = record;

		subdirectories = record.Subdirectories.ToDictionary(
			subdirRecord => subdirRecord.Name,
			subdirRecord => new PackedArchiveDirectory(archive, subdirRecord));

		files = record.Files.ToDictionary(
			fileRecord => fileRecord.Name,
			fileRecord => new PackedArchiveFile(archive, fileRecord));
	}
	
	public string Name => record.Name;

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
