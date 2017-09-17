using SharpDX;
using static System.Math;
using static MathExtensions;

public static class ScatteringProfileExtensions {
	public const double SubdivisionThreshold = 1e-3;
	public const double PerceptibilityThreshold = 1e-5;

	public const double MinDistanceForSearch = 0.01;
	public const double InitialDistanceForSearch = 1; //in same units as profile (usually cm)
	public const double DistanceSearchStep = 1.3;

	/**
	 *  Finds a distance beyond which the maximum contribution is always imperceptible.
	 */
	public static double FindImperceptibleDistance(this ScatteringProfile profile, double area) {
		double currentDistance = 1;
		double currentContribution = profile.CalculateMaximumContribution(currentDistance, area);

		if (currentContribution < PerceptibilityThreshold) {
			//search smaller distances until we get to perceptibility
			while (currentContribution < PerceptibilityThreshold) {
				currentDistance = currentDistance / DistanceSearchStep;
				if (currentDistance < MinDistanceForSearch) {
					break;
				}

				currentContribution = profile.CalculateMaximumContribution(currentDistance, area);
			}
			
			//the current contribution is just perceptible, so take one step back up to get to imperceptible
			return currentDistance * DistanceSearchStep;
		} else {
			//search larger distances until we get to imperceptibility
			while (currentContribution >= PerceptibilityThreshold) {
				currentDistance = currentDistance * DistanceSearchStep;
				currentContribution = profile.CalculateMaximumContribution(currentDistance, area);
			}

			return currentDistance;
		}
	}

	public static double CalculateMaximumContribution(this ScatteringProfile profile, double nearRadius, double area) {
		double farRadius = Sqrt(Sqr(nearRadius) + area / PI);
		return profile.EvaluateCDF(farRadius) - profile.EvaluateCDF(nearRadius);
	}
		
	public static Vector4 IntegrateOverQuad(this ScatteringProfile profile, Vector3 receiverPosition, PositionedQuad quad) {
		int nearestIdx = quad.FindClosestCorner(receiverPosition);
		
		SubQuad orientedSubQuad = SubQuad.MakeRotatedWhole(nearestIdx);
		
		return IntegrateOverSubQuad(profile, receiverPosition, quad, orientedSubQuad);
	}

	private static Vector4 IntegrateOverSubQuad(ScatteringProfile profile, Vector3 receiverPosition, PositionedQuad root, SubQuad subQuad) {
		var quad = subQuad.AsPositionedQuad(root);
		double area = quad.Area;

		//this assume that P0 is the closest point to the receiver
		double closestDistance = Vector3.Distance(receiverPosition, quad.P0);
		double maximumContribution = profile.CalculateMaximumContribution(closestDistance, area);

		if (maximumContribution < SubdivisionThreshold) {
			double distance = Vector3.Distance(receiverPosition, quad.Center);
			return subQuad.CenterWeight * (float) (area * profile.CalculateDiffuseReflectance(distance));
		} else {
			Vector4 total = Vector4.Zero;
			foreach (SubQuad subSubQuad in subQuad.Split()) {
				total += IntegrateOverSubQuad(profile, receiverPosition, root, subSubQuad);
			}
			return total;
		}
	}
}
