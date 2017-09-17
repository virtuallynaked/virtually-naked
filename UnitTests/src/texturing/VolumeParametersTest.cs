using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class VolumeParametersTest {
	[TestMethod]
	public void TestSurfaceAlbedo() {
		//parameters from Eva 7 Torso surface
		double transmittedDistanceOfMeasurement = 0.3;
		double transmittedColor = 0.95f;
		double scatteringDistanceOfMeasurement = 0.12;
		double subSurfaceScatteringAmount = 0.69;
		double subSurfaceScatteringDirection = -0.650;

		var volumeParameters = new VolumeParameters(
			transmittedDistanceOfMeasurement,
			transmittedColor,
			scatteringDistanceOfMeasurement,
			subSurfaceScatteringAmount,
			subSurfaceScatteringDirection);
		Assert.AreEqual(0.75678f, volumeParameters.SurfaceAlbedo, 5e-6);
	}
}
