using ProtoBuf.Meta;
using SharpDX;
using System;
using System.IO;

public static class Persistance {
	private static RuntimeTypeModel typeModel;

	static Persistance() {
		typeModel = RuntimeTypeModel.Create();
		typeModel.Add(typeof(Vector3), false).Add("X", "Y", "Z");
		typeModel.Add(typeof(Vector2), false).Add("X", "Y");
		typeModel.Add(typeof(Quaternion), false).Add("X", "Y", "Z", "W");
	}

	public static void Write<T>(Stream stream, T t) {
		typeModel.Serialize(stream, t);
	}

	public static void Save<T>(FileInfo file, T t) {
		try {
			using (var stream = file.Create()) {
				Write(stream, t);
			}
		} catch (Exception) {
			file.Delete();
			throw;
		}
	}

	public static T Read<T>(Stream stream) {
		return (T) typeModel.Deserialize(stream, null, typeof(T));
	}

	public static T Load<T>(IArchiveFile file) {
		using (var stream = file.OpenRead()) {
			return Read<T>(stream);
		}
	}

	public static T Load<T>(FileInfo file) {
		using (var stream = file.OpenRead()) {
			return Read<T>(stream);
		}
	}
}
