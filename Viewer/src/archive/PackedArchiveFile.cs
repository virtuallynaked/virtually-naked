using SharpDX;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;

public class PackedArchiveFileDataView : IArchiveFileDataView {
	private readonly MemoryMappedViewAccessor accessor;
	private readonly DataPointer dataPointer;
	
	public PackedArchiveFileDataView(MemoryMappedViewAccessor accessor, long size) {
		this.accessor = accessor;

		unsafe {
			byte* ptr = null;
			accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
			ptr += accessor.PointerOffset;
			dataPointer = new DataPointer((IntPtr) ptr, (int) size);
		}
	}

	public DataPointer DataPointer => dataPointer;

	public void Dispose() {
		accessor.SafeMemoryMappedViewHandle.ReleasePointer();
		accessor.Dispose();
	}
}

public class PackedArchiveFile : IArchiveFile {
	private readonly PackedArchive archive;
	private readonly PackedArchiveFileRecord record;

	public PackedArchiveFile(PackedArchive archive, PackedArchiveFileRecord record) {
		this.archive = archive;
		this.record = record;
	}

	public string Name => record.Name;

	public IArchiveFileDataView OpenDataView() {
		return new PackedArchiveFileDataView(archive.OpenAccessor(record), record.Size);
	}

	public Stream OpenRead() {
		return archive.OpenStream(record);
	}

	public byte[] ReadAllBytes() {
		byte[] bytes = new byte[record.Size];
		using (var stream = OpenRead()) {
			stream.Read(bytes, 0, bytes.Length);
		}
		return bytes;
	}

	public T[] ReadArray<T>() where T : struct {
		using (var accessor = archive.OpenAccessor(record)) {
			return accessor.ReadWholeArray<T>(record.Size);
		}
	}
}