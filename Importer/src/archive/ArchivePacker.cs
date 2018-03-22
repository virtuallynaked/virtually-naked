using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;

public class ArchivePacker {
	private const long HeaderSize = sizeof(long) + sizeof(long);
	private const long OffsetGranularity = 0x1000;

	private long currentOffset = 0;

	private PackedArchiveFileRecord ScanFile(FileInfo file) {
		string name = file.Name;
		long offset = currentOffset;
		long size = file.Length;
		
		IncrementOffset(size);

		return new PackedArchiveFileRecord(name, offset, size);
	}

	private void IncrementOffset(long size) {
		currentOffset = IntegerUtils.NextLargerMultiple(currentOffset + size, OffsetGranularity);
	}

	private PackedArchiveDirectoryRecord ScanDirectory(DirectoryInfo dir) {
		string name = dir.Name;

		var subdirectories = dir.GetDirectories()
			.Select(subDir => ScanDirectory(subDir))
			.ToArray();

		var files = dir.GetFiles()
			.Select(file => ScanFile(file))
			.ToArray();

		return new PackedArchiveDirectoryRecord(name, subdirectories, files);
	}
	
	public void Pack(FileInfo archiveFile, DirectoryInfo rootDir) {
		var rootRecord = ScanDirectory(rootDir);

		var memoryStream = new MemoryStream();
		Persistance.Write(memoryStream, rootRecord);
		var listingBytes = memoryStream.ToArray();
		long listingSize = listingBytes.LongLength;
		
		long payloadSize = currentOffset;
		long payloadOffset = IntegerUtils.NextLargerMultiple(HeaderSize + listingSize, OffsetGranularity);

		long totalSize = payloadOffset + payloadSize;

		using (var archiveMap = MemoryMappedFile.CreateFromFile(archiveFile.FullName, FileMode.Create, archiveFile.Name, totalSize)) {
			//write header
			using (var headerAccessor = archiveMap.CreateViewAccessor(0, HeaderSize)) {
				headerAccessor.Write(0, listingSize);
				headerAccessor.Write(sizeof(long), payloadOffset);
			}

			//write listing
			using (var listingAccessor = archiveMap.CreateViewAccessor(HeaderSize, listingSize)) {
				listingAccessor.WriteArray(0, listingBytes, 0, listingBytes.Length);
			}

			WriteDirectory(archiveMap, payloadOffset, rootRecord, rootDir);
		}
	}

	private void WriteDirectory(MemoryMappedFile archiveMap, long payloadOffset, PackedArchiveDirectoryRecord record, DirectoryInfo dir) {
		foreach (var subdirRecord in record.Subdirectories) {
			WriteDirectory(archiveMap, payloadOffset, subdirRecord, dir.Subdirectory(subdirRecord.Name));
		}
		foreach (var fileRecord in record.Files) {
			WriteFile(archiveMap, payloadOffset, fileRecord, dir.File(fileRecord.Name));
		}
	}

	private void WriteFile(MemoryMappedFile archiveMap, long payloadOffset, PackedArchiveFileRecord fileRecord, FileInfo file) {
		Console.WriteLine("packing " + file.Name + "...");

		long offset = payloadOffset + fileRecord.Offset;
		long size = fileRecord.Size;
		using (var sourceStream = file.OpenRead()) {
			using (var destStream = archiveMap.CreateViewStream(offset, size)) {
				sourceStream.CopyTo(destStream);
			}
		}
	}
}
