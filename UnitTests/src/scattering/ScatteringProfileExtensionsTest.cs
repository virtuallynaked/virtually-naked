using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpDX;

[TestClass]
public class ScatteringProfileExtensionsTest {
	[TestMethod]
	public void TestFindImperceptibleDistanceSearchingUp() {
		var profile = new ScatteringProfile(1, 1e-1);
		double area = 1;

		Assert.IsTrue(profile.CalculateMaximumContribution(ScatteringProfileExtensions.InitialDistanceForSearch, area) >= ScatteringProfileExtensions.PerceptibilityThreshold,
			"search will be up");

		double distance = profile.FindImperceptibleDistance(area);

		Assert.IsTrue(profile.CalculateMaximumContribution(distance, area) < ScatteringProfileExtensions.PerceptibilityThreshold);
		Assert.IsTrue(profile.CalculateMaximumContribution(distance / ScatteringProfileExtensions.DistanceSearchStep, area) >= ScatteringProfileExtensions.PerceptibilityThreshold);
	}

	[TestMethod]
	public void TestFindImperceptibleDistanceSearchingDown() {
		var profile = new ScatteringProfile(1, 1e-2);
		double area = 1;

		Assert.IsTrue(profile.CalculateMaximumContribution(ScatteringProfileExtensions.InitialDistanceForSearch, area) < ScatteringProfileExtensions.PerceptibilityThreshold,
			"search will be down");

		double distance = profile.FindImperceptibleDistance(area);

		Assert.IsTrue(profile.CalculateMaximumContribution(distance, area) < ScatteringProfileExtensions.PerceptibilityThreshold);
		Assert.IsTrue(profile.CalculateMaximumContribution(distance / ScatteringProfileExtensions.DistanceSearchStep, area) >= ScatteringProfileExtensions.PerceptibilityThreshold);
	}

	[TestMethod]
	public void TestIntegrateOverQuad() {
		var profile = new ScatteringProfile(1, 0.1);
		Vector3 receiverPosition = new Vector3(0, 0, 0);
		float sideLength = 100;
		var quad = new PositionedQuad(
			new Vector3(sideLength, sideLength, 0),
			new Vector3(0, sideLength, 0),
			new Vector3(0, 0, 0),
			new Vector3(sideLength, 0, 0)
		);
		
		Vector4 contribution = profile.IntegrateOverQuad(receiverPosition, quad);

		//quad should cover 1/4 of total area
		//only vertex 2 is close, so contributions from other vertices should be negligible
		Assert.AreEqual(0.25, contribution[2], 5e-3);
		Assert.AreEqual(0, contribution[0], 5e-3);
		Assert.AreEqual(0, contribution[1], 5e-3);
		Assert.AreEqual(0, contribution[3], 5e-3);
	}
}
