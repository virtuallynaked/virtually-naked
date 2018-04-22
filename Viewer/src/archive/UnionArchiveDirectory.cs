using System.Collections.Generic;
using System.Linq;

public class UnionArchiveDirectory : IArchiveDirectory {
	public static UnionArchiveDirectory Join(string name, IEnumerable<IArchiveDirectory> directories) {
		var subdirectories = directories
			.SelectMany(dir => dir.Subdirectories)
			.GroupBy(subDir => subDir.Name)
			.Select(grouping => Join(grouping.Key, grouping))
			.ToDictionary<IArchiveDirectory, string>(subDir => subDir.Name);

		var files = directories
			.SelectMany(dir => dir.GetFiles())
			.GroupBy<IArchiveFile, string>(file => file.Name)
			.ToDictionary(grouping => grouping.Key, grouping => grouping.Last());

		return new UnionArchiveDirectory(name, subdirectories, files);
	}

	private readonly string name;
	private readonly Dictionary<string, IArchiveDirectory> subdirectories;
	private readonly Dictionary<string, IArchiveFile> files;

	public UnionArchiveDirectory(string name, Dictionary<string, IArchiveDirectory> subdirectories, Dictionary<string, IArchiveFile> files) {
		this.name = name;
		this.subdirectories = subdirectories;
		this.files = files;
	}

	public string Name => name;

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
