using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class RegressionResult2D {
	public readonly double b1;
	public readonly double b2;
	
	public RegressionResult2D(double b1, double b2) {
		this.b1 = b1;
		this.b2 = b2;
	}
	
	public double Eval(double x1, double x2) {
		return b1 * x1 + b2 * x2;
	}

	public override String ToString() {
		return $"RegressionResult2D [b1={b1}, b2={b2}]";
	}
}


public class WeightedLeastSquaresRegressor2D {
	private double sumX1X1 = 0;
	private double sumX1X2 = 0;
	private double sumX2X2 = 0;
	private double sumX1Y = 0;
	private double sumX2Y = 0;
	
	public void AddData(double x1, double x2, double y, double weight) {
		double x1w = x1 * weight;
		double x2w = x2 * weight;
		
		sumX1X1 += x1w * x1;
		sumX1X2 += x1w * x2;
		sumX2X2 += x2w * x2;
		sumX1Y += x1w * y;
		sumX2Y += x2w * y;
	}
	
	public RegressionResult2D GetResult() {
		double denom = sumX1X2 * sumX1X2 - sumX1X1 * sumX2X2;
		
		double b1 = (sumX1X2 * sumX2Y - sumX2X2 * sumX1Y) / denom;
		double b2 = (sumX1X2 * sumX1Y - sumX1X1 * sumX2Y) / denom;
		
		return new RegressionResult2D(b1, b2);
	}
}
