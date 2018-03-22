using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Direct3D11;

public static class IntegerUtils {
	public static int RoundUp(int num, int divisor) {
		return (num + divisor - 1) / divisor;
	}

	public static long RoundUp(long num, long divisor) {
		return (num + divisor - 1) / divisor;
	}

	public static int Mod(int num, int modulus) {
		int remainder = num % modulus;
		return remainder >= 0 ? remainder : remainder + modulus;
	}

	public static int NextLargerMultiple(int num, int m) {
		return RoundUp(num, m) * m;
	}

	public static long NextLargerMultiple(long num, long m) {
		return RoundUp(num, m) * m;
	}

	public static ushort ToUShort(float f) {
		f *= ushort.MaxValue + 1;
		if (f < 0) {
			return 0;
		} else if (f >= (float) (ushort.MaxValue + 1)) {
			return ushort.MaxValue;
		} else {
			return (ushort) f;
		}
	}
	
	public static float FromUShort(ushort value) {
		return (value + 0.5f) / (float) (ushort.MaxValue + 1);
	}

	public static uint Pack(ushort lower, ushort upper) {
		return ((uint) upper << 16) | lower;
	}

	public static void Unpack(uint combined, out ushort lower, out ushort upper) {
		lower = (ushort) combined;
		upper = (ushort) (combined >> 16);
	}

	public static int Clamp(int x, int min, int max) {
		if (x < min) {
			return min;
		} else if (x > max) {
			return max;
		} else {
			return x;
		}
	}
}
