using static System.Math;
using static MathExtensions;

public class ScatteringProfile {
	public readonly double surfaceAlbedo; //A
	public readonly double meanFreePath; //l
	public readonly double shapeParameter; //d

	public ScatteringProfile(double surfaceAlbedo, double meanFreePath) {
		this.surfaceAlbedo = surfaceAlbedo;
		this.meanFreePath = meanFreePath;

		double s = 1.9 - surfaceAlbedo + 3.5 * Sqr(surfaceAlbedo - 0.8);
		shapeParameter = meanFreePath / s;
	}

	private double CalculateQ(double r, double t) {
		return (Exp(-r/t))/(2*PI*t*r);
	}

	public double CalculateDiffuseReflectance(double radius) {
		return surfaceAlbedo * (
			1/4d * CalculateQ(radius, shapeParameter) +
			3/4d * CalculateQ(radius, shapeParameter * 3));
	}

	private double EvaluateQCDF(double r, double t) {
		return Exp(-r/t);
	}

	public double EvaluateCDF(double radius) {
		return 1 - 1/4d * EvaluateQCDF(radius, shapeParameter) - 3/4d * EvaluateQCDF(radius, shapeParameter * 3);
	}
}
