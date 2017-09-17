using System.Collections.Generic;
using System.IO;

public interface IArchiveDirectory {
	string Name { get; }
	IArchiveDirectory Subdirectory(string name);
	IArchiveFile File(string name);
	IEnumerable<IArchiveFile> GetFiles();
	IEnumerable<IArchiveDirectory> Subdirectories { get; }
}
