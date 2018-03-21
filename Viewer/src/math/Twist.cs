using SharpDX;
using static System.Math;
using static MathExtensions;

public struct Twist {
	private readonly float x; //expressed as Sin[angle/2]

	public Twist(float z) {
		this.x = z;
	}
	
	public float X => x;
	public float WSquared => 1 - Sqr(x);

	public float W {
		get {
			float wSquared = WSquared;
			return wSquared > 0 ? (float) Sqrt(wSquared) : 0;
		}
	}

	public float Angle => 2 * (float) Asin(x);

	public static readonly Twist Zero = new Twist(0);

	override public string ToString() {
		return string.Format("Twist[{0}]", X);
	}

	public static Twist MakeFromAngle(float twistAngle) {
		twistAngle = (float) IEEERemainder(twistAngle, 2 * PI);
		float x = (float) Sin(twistAngle / 2);
		return new Twist(x);
	}

	public Quaternion AsQuaternion(CartesianAxis twistAxis) {
		Quaternion q = default(Quaternion);
		q[(int) twistAxis] = X;
		q.W = W;
		return q;
	}

	private static Twist Add(float w1, float x1, float w2, float x2) {
		float w = w1 * w2 - x1 * x2;
		float x = w2 * x1 + w1 * x2;

		if (w < 0) {
			w = -w;
			x = -x;
		}

		return new Twist(x);
	}

	public static Twist operator+(Twist t1, Twist t2) {
		return Add(t1.W, t1.X, t2.W, t2.X);
	}

	public static Twist operator-(Twist t1, Twist t2) {
		return Add(t1.W, t1.X, -t2.W, t2.X);
	}
}
