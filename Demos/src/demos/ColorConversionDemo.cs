using SharpDX;
using System;

public class ColorConversionDemo {
	public void Run() {
		foreach (double temperature in new[] {2500, 5500, 6500, 10000}) {
			Vector2 xy = ColorConversion.FromTemperatureToCIExy(temperature);
			Vector3 XYZ = ColorConversion.FromCIExyTOCIEXYZ(xy, 1);
			Vector3 RGB = ColorConversion.FromCIEXYZtoLinearSRGB(XYZ);

			Console.WriteLine("temperature = {0}", temperature);
			Console.WriteLine("CIE xy = ({0}, {1})", xy[0], xy[1]);
			Console.WriteLine("CIE XYZ = ({0}, {1}, {2})", XYZ[0], XYZ[1], XYZ[2]);
			Console.WriteLine("Linear sRGB = ({0}, {1}, {2})", RGB[0], RGB[1], RGB[2]);
			Console.WriteLine();
		}
	}
}
