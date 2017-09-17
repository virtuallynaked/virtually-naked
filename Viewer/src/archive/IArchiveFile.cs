using SharpDX;
using System;
using System.IO;

public interface IArchiveFileDataView : IDisposable {
	DataPointer DataPointer { get; }
}

public interface IArchiveFile {
	string Name { get; }
	Stream OpenRead();
	IArchiveFileDataView OpenDataView();
	byte[] ReadAllBytes();
	T[] ReadArray<T>() where T : struct;
}
