using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using SharpDX;

public class UnpackedArchiveFileDataView : IArchiveFileDataView {
	private readonly MemoryMappedFile map;
	private readonly MemoryMappedViewAccessor accessor;
	private readonly DataPointer dataPointer;
	
	public UnpackedArchiveFileDataView(FileInfo file) {
		long size = file.Length;
		map = file.OpenMemoryMappedFileForSharedRead();
		accessor = map.CreateViewAccessor(0, size, MemoryMappedFileAccess.Read);

		unsafe {
			byte* ptr = null;
			accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
			dataPointer = new DataPointer((IntPtr) ptr, (int) size);
		}
	}

	public DataPointer DataPointer => dataPointer;

	public void Dispose() {
		accessor.SafeMemoryMappedViewHandle.ReleasePointer();
		accessor.Dispose();
		map.Dispose();
	}
}

public class UnpackedArchiveFile : IArchiveFile {
	public static UnpackedArchiveFile Make(FileInfo info) {
		return info.Exists ? new UnpackedArchiveFile(info) : null;
	}

	private readonly FileInfo info;

	private UnpackedArchiveFile(FileInfo info) {
		this.info = info;
	}

	public string Name => info.Name;

	public Stream OpenRead() {
		return info.OpenRead();
	}

	public byte[] ReadAllBytes() {
		return info.ReadAllBytes();
	}

	public T[] ReadArray<T>() where T : struct {
		return info.ReadArray<T>();
	}

	public IArchiveFileDataView OpenDataView() {
		return new UnpackedArchiveFileDataView(info);
	}
}
