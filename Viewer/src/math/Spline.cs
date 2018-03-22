using ProtoBuf;

public class Spline {
	public struct Knot {
		public double Position { get; }
		public double Value { get; }

		public Knot(double position, double value) {
			Position = position;
			Value = value;
		}
	}

	private Knot[] knots;

	public Spline(Knot[] knots) {
		this.knots = knots;
	}
	
	public Knot[] Knots => knots;

	private double EvalSegment(double x, int segmentIdx) {
		double loc = Knots[segmentIdx].Position;
		double scale = Knots[segmentIdx + 1].Position - Knots[segmentIdx].Position;
			
		double p0 = Knots[segmentIdx].Value;
		double p1 = Knots[segmentIdx + 1].Value;

		double m0;
		if (segmentIdx == 0) {
			m0 = 0;
		} else {
			m0 = (Knots[segmentIdx + 1].Value - Knots[segmentIdx - 1].Value) / (Knots[segmentIdx + 1].Position - Knots[segmentIdx - 1].Position) * scale;
		}

		double m1;
		if (segmentIdx == Knots.Length - 2) {
			m1 = 0;
		} else {
			m1 = (Knots[segmentIdx + 2].Value - Knots[segmentIdx].Value) / (Knots[segmentIdx + 2].Position - Knots[segmentIdx].Position) * scale;
		}

		double t = (x - loc) / (scale);
		double t1 = t;
		double t2 = t1 * t;
		double t3 = t2 * t;
			
		double y = (2 * t3 - 3 * t2 + 1) * p0
			+ (t3 - 2 * t2 + t1) * m0
			+ (-2 * t3 + 3 * t2) * p1
			+ (t3 - t2) * m1;
			
		return y;
	}

	public double Eval(double x) {
		int knotCount = Knots.Length;

		if (x < Knots[0].Position) {
			return Knots[0].Value;
		} else if (x >= Knots[knotCount - 1].Position) {
			return Knots[knotCount - 1].Value;
		} else {
			for (int i = 0; i < knotCount; ++i) {
				if (x >= Knots[i].Position && x < Knots[i + 1].Position) {
					return EvalSegment(x, i);
				}
			}
		}

		return 0;
	}
}
