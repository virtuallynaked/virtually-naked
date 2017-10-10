using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct OcclusionInfo {
	public static readonly int SizeInBytes = 2 * sizeof(float);
	public static readonly int PackedSizeInBytes = sizeof(uint);

	public float Front { get; }
	public float Back { get; }

	public OcclusionInfo(float front, float back) {
		Front = front;
		Back = back;
	}

	public static OcclusionInfo Zero {
		get {
			return new OcclusionInfo(0, 0);
		}
	}

	public static OcclusionInfo operator *(float f, OcclusionInfo info) {
		return new OcclusionInfo(f * info.Front,	f * info.Back);
	}

	public static OcclusionInfo operator +(OcclusionInfo info1, OcclusionInfo info2) {
		return new OcclusionInfo(info1.Front + info2.Front, info1.Back + info2.Back);
	}

	
	public static uint Pack(OcclusionInfo occlusionInfo) {
		return IntegerUtils.Pack(
			IntegerUtils.ToUShort(occlusionInfo.Front),
			IntegerUtils.ToUShort(occlusionInfo.Back));
	}

	public static OcclusionInfo Unpack(uint packedOcclusionInfo) {
		IntegerUtils.Unpack(packedOcclusionInfo, out ushort front, out ushort back);
		return new OcclusionInfo(IntegerUtils.FromUShort(front), IntegerUtils.FromUShort(back));
	}
	
	public static OcclusionInfo[] UnpackArray(uint[] packedArray) {
		if (packedArray == null) {
			return null;
		}

		int length = packedArray.Length;
		OcclusionInfo[] array = new OcclusionInfo[length];
		for (int i = 0; i < length; ++i) {
			array[i] = Unpack(packedArray[i]);
		}
		return array;
	}

	public static uint[] PackArray(OcclusionInfo[] array) {
		if (array == null) {
			return null;
		}

		int length = array.Length;
		uint[] packedArray = new uint[length];
		for (int i = 0; i < length; ++i) {
			packedArray[i] = Pack(array[i]);
		}
		return packedArray;
	}

	override public string ToString() {
		return String.Format("OcclusionInfo[front = {0}, back = {1}]", Front, Back);
	}
}
