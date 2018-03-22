using System;
using System.IO;
using System.IO.MemoryMappedFiles;

public class PackedArchiveFileRecord {
	public string Name { get; }
	public long Offset { get; }
	public long Size { get; }

	public PackedArchiveFileRecord(string name, long offset, long size) {
		Name = name;
		Offset = offset;
		Size = size;
	}
}

public class PackedArchiveDirectoryRecord {
	public string Name { get; }
	public PackedArchiveDirectoryRecord[] Subdirectories { get; }
	public PackedArchiveFileRecord[] Files { get; }

	public PackedArchiveDirectoryRecord(string name, PackedArchiveDirectoryRecord[] subdirectories, PackedArchiveFileRecord[] files) {
		Name = name;
		Subdirectories = subdirectories ?? new PackedArchiveDirectoryRecord[0];
		Files = files ?? new PackedArchiveFileRecord[0];
	}
}

public class PackedArchive : IDisposable {
	public const long HeaderSize = sizeof(long) + sizeof(long);

	private readonly MemoryMappedFile map;
	private readonly long payloadOffset;
	private readonly PackedArchiveDirectory root;

	public PackedArchive(FileInfo file) {
		map = file.OpenMemoryMappedFileForSharedRead();

		long listingSize;
		using (var headerAccessor = map.CreateViewAccessor(0, HeaderSize, MemoryMappedFileAccess.Read)) {
			listingSize = headerAccessor.ReadInt64(0);
			payloadOffset = headerAccessor.ReadInt64(sizeof(long));
		}

		PackedArchiveDirectoryRecord rootRecord;
		using (var listingStream = map.CreateViewStream(HeaderSize, listingSize, MemoryMappedFileAccess.Read)) {
			rootRecord = Persistance.Read<PackedArchiveDirectoryRecord>(listingStream);
		}

		root = new PackedArchiveDirectory(this, rootRecord);
	}


	public PackedArchiveDirectory Root => root;

	public void Dispose() {
		map.Dispose();
	}

	public Stream OpenStream(PackedArchiveFileRecord record) {
		return map.CreateViewStream(payloadOffset + record.Offset, record.Size, MemoryMappedFileAccess.Read);
	}

	public MemoryMappedViewAccessor OpenAccessor(PackedArchiveFileRecord record) {
		return map.CreateViewAccessor(payloadOffset + record.Offset, record.Size, MemoryMappedFileAccess.Read);
	}
}
