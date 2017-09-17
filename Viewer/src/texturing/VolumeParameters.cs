using SharpDX;
using static System.Math;

public class VolumeParameters {
	public VolumeParameters(
		double transmittedDistanceOfMeasurement, //distance-units
		double transmittedColor,
		double scatteringDistanceOfMeasurement, //distance-units
		double subSurfaceScatteringAmount,
		double subSurfaceScatteringDirection) {
		TransmittedDistanceOfMeasurement = transmittedDistanceOfMeasurement;
		TransmittedColor = transmittedColor;
		ScatteringDistanceOfMeasurement = scatteringDistanceOfMeasurement;
		SubSurfaceScatteringAmount = subSurfaceScatteringAmount;
		SubSurfaceScatteringDirection = subSurfaceScatteringDirection;
	}

	/* 
	 * Note on dimensions:
	 *    DistanceOfMeasurement and and Length properties have a dimension of distance-units
	 *    Coefficient properties have a dimension of distance-units^-1
	 */

	public double TransmittedDistanceOfMeasurement { get; }
	public double TransmittedColor { get; }
	public double ScatteringDistanceOfMeasurement { get; }
	public double SubSurfaceScatteringAmount { get; }
	public double SubSurfaceScatteringDirection { get; } //G aka directional bias
	
	public double ScatteringCoefficient => SubSurfaceScatteringAmount / ScatteringDistanceOfMeasurement; //sigma_s (in distance-units^-1)
	public double AbsorptionCoefficient => -Log(TransmittedColor) / TransmittedDistanceOfMeasurement; //sigma_a

	public double SurfaceAlbedo {
		get {
			/* 
			 * Surface albedo is a physically a function of single scattering albedo, but this unfortunately this function
			 * doesn't have a closed form. Insteadm the formula below is rather a curve-fit approximation to outputs from
			 * the Daz Studio IRay.
			 */
			double z = 1 / SingleScatteringAlbedo - 1;
			return 0.92 / (1 + 5.601740802787776 * Pow(z, 0.8109772916404087));
		}
	}

	public double ReducedScatteringCoefficient => ScatteringCoefficient * ( 1 - SubSurfaceScatteringDirection); //sigma'_s
	public double ReducedExtinctionCoefficient => ReducedScatteringCoefficient + AbsorptionCoefficient; //sigma'_t
	public double MeanFreePathLength => 1 / ReducedExtinctionCoefficient; //l_u
	public double SingleScatteringAlbedo => ReducedScatteringCoefficient / ReducedExtinctionCoefficient;
}
