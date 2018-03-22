using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

public static class SystemIOExtensions {
	public static FileInfo File(this DirectoryInfo directoryInfo, string fileName) {
		string fullFileName = Path.Combine(directoryInfo.FullName, fileName);
		return new FileInfo(fullFileName);
	}

	public static DirectoryInfo Subdirectory(this DirectoryInfo directoryInfo, string fileName) {
		string fullFileName = Path.Combine(directoryInfo.FullName, fileName);
		return new DirectoryInfo(fullFileName);
	}

	public static void CreateWithParents(this DirectoryInfo directoryInfo) {
		Directory.CreateDirectory(directoryInfo.FullName);
	}

	public static byte[] ReadAllBytes(this FileInfo fileInfo) {
		return System.IO.File.ReadAllBytes(fileInfo.FullName);
	}

	public static string ReadAllText(this FileInfo fileInfo) {
		return System.IO.File.ReadAllText(fileInfo.FullName);
	}

	public static void WriteAllBytes(this FileInfo fileInfo, byte[] bytes) {
		System.IO.File.WriteAllBytes(fileInfo.FullName, bytes);
	}

	public static void WriteAllText(this FileInfo fileInfo, string text) {
		System.IO.File.WriteAllText(fileInfo.FullName, text);
	}

	public static void WriteSerializable(this FileInfo fileInfo, object obj) {
		using (var stream = System.IO.File.OpenWrite(fileInfo.FullName)) {
			BinaryFormatter formatter = new BinaryFormatter();
			formatter.Serialize(stream, obj);
		}
	}

	public static void ReadSerializable(this FileInfo fileInfo) {
		using (var stream = System.IO.File.OpenRead(fileInfo.FullName)) {
			BinaryFormatter formatter = new BinaryFormatter();
			formatter.Deserialize(stream);
		}
	}

	public static void WriteArray<T>(this FileInfo fileInfo, T[] array) where T : struct {
		int elementSize = Marshal.SizeOf<T>();
		int elementCount = array.Length;
		int arraySize = elementCount * elementSize;

		if (elementCount == 0) {
			fileInfo.Create().Close();
			return;
		}

		using (var memoryMap = MemoryMappedFile.CreateFromFile(fileInfo.FullName, FileMode.Create, fileInfo.Name, arraySize)) {
			using (var accessor = memoryMap.CreateViewAccessor()) {
				accessor.WriteArray(0, array, 0, elementCount);
			}
		}
	}

	public static T[] ReadWholeArray<T>(this MemoryMappedViewAccessor accessor, long arraySize) where T : struct {
		int elementSize = Marshal.SizeOf<T>();
		if (arraySize % elementSize != 0) {
			throw new InvalidOperationException("file size is not a multiple of element size");
		}
		int elementCount = Convert.ToInt32(arraySize / elementSize);
				
		T[] array = new T[elementCount];

		if (elementCount == 0) {
			return array;
		}

		accessor.ReadArray<T>(0, array, 0, elementCount);

		return array;
	}

	public static T[] ReadArray<T>(this FileInfo fileInfo) where T : struct {
		if (fileInfo.Length == 0) {
			return new T[0];
		}

		using (var memoryMap = fileInfo.OpenMemoryMappedFileForSharedRead()) {
			using (var accessor = memoryMap.CreateViewAccessor(0, fileInfo.Length, MemoryMappedFileAccess.Read)) {
				return accessor.ReadWholeArray<T>(fileInfo.Length);
			}
		}
	}

	public static MemoryMappedFile OpenMemoryMappedFileForSharedRead(this FileInfo fileInfo) {
		var stream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
		var map = MemoryMappedFile.CreateFromFile(stream, Guid.NewGuid().ToString(), fileInfo.Length, MemoryMappedFileAccess.Read, null, HandleInheritability.None, false);
		return map;
	}

	public static string GetNameWithoutExtension(this FileInfo fileInfo) {
		return Path.GetFileNameWithoutExtension(fileInfo.FullName);
	}
}
