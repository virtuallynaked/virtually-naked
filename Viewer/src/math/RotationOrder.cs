using SharpDX;
using System;

public struct RotationOrder {
	//http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/Quaternions.pdf

	private const int X = 0;
    private const int Y = 1;
    private const int Z = 2;

	private readonly static Vector3[] UnitVectors = new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ };

	public readonly static RotationOrder XYZ = new RotationOrder(X, Y, Z);
    public readonly static RotationOrder XZY = new RotationOrder(X, Z, Y);
    public readonly static RotationOrder YXZ = new RotationOrder(Y, X, Z);
    public readonly static RotationOrder YZX = new RotationOrder(Y, Z, X);
    public readonly static RotationOrder ZXY = new RotationOrder(Z, X, Y);
    public readonly static RotationOrder ZYX = new RotationOrder(Z, Y, X);

    public static RotationOrder DazStandard = XYZ;

    private static int AxisIdFromChar(char ch) {
        if (ch < 'X' || ch > 'Z') {
            throw new ArgumentException("not a valid axis: " + ch);
        }

        return ch - 'X';
    }

    public static RotationOrder FromString(string str) {
        if (str.Length != 3) {
            throw new ArgumentException("not a valid axis order: " + str);
        }

        int primary = AxisIdFromChar(str[0]);
        int secondary = AxisIdFromChar(str[1]);
        int tertiary = AxisIdFromChar(str[2]);
        return new RotationOrder(primary, secondary, tertiary);
    }
    
    public readonly int primaryAxis;
    public readonly int secondaryAxis;
    public readonly int tertiaryAxis;
    private readonly bool circular;

    public RotationOrder(int primaryAxis, int secondaryAxis, int tertiaryAxis) {
        this.primaryAxis = primaryAxis;
        this.secondaryAxis = secondaryAxis;
        this.tertiaryAxis = tertiaryAxis;

		circular =
			(primaryAxis == 0 && secondaryAxis == 1) ||
			(primaryAxis == 1 && secondaryAxis == 2) ||
			(primaryAxis == 2 && secondaryAxis == 0); 
    }

	public Quaternion FromTwistSwingAngles(Vector3 angles) {
		Quaternion twistQ = Quaternion.RotationAxis(UnitVectors[primaryAxis], angles[primaryAxis]);
		
		Vector3 swingVector = angles;
		swingVector[primaryAxis] = 0;
		
		float swingAngle = swingVector.Length();
		Quaternion swingQ = Quaternion.RotationAxis(swingVector, swingAngle);

		return twistQ.Chain(swingQ);
	}

	public Vector3 ToTwistSwingAngles(Quaternion quaternion) {
		quaternion.DecomposeIntoTwistThenSwing(UnitVectors[primaryAxis], out Quaternion twistQ, out Quaternion swingQ);

		float swingAngle = swingQ.Angle;
		Vector3 swingAxis = swingQ.Axis;

		Vector3 angles = default(Vector3);
		angles[primaryAxis] = twistQ.Angle * twistQ.Axis[primaryAxis];
		angles[secondaryAxis] = swingAngle * swingAxis[secondaryAxis];
		angles[tertiaryAxis] = swingAngle * swingAxis[tertiaryAxis];

		return angles;
	}

    public Quaternion FromEulerAngles(Vector3 angles) {
		double halfAngle1 = angles[primaryAxis] / 2;
		double halfAngle2 = angles[secondaryAxis] / 2;
		double halfAngle3 = angles[tertiaryAxis] / 2;
		double e = circular ? -1 : +1;
		
		double sin1 = Math.Sin(halfAngle1);
		double cos1 = Math.Cos(halfAngle1);
		double sin2 = Math.Sin(halfAngle2);
		double cos2 = Math.Cos(halfAngle2);
		double sin3 = Math.Sin(halfAngle3);
		double cos3 = Math.Cos(halfAngle3);
				
		double p0 = cos3 * cos2 * cos1 - e * sin3 * sin2 * sin1;
		double p1 = cos3 * cos2 * sin1 + e * sin3 * sin2 * cos1;
		double p2 = cos3 * sin2 * cos1 - e * sin3 * cos2 * sin1;
		double p3 = sin3 * cos2 * cos1 + e * cos3 * sin2 * sin1;

		Quaternion q = default(Quaternion);
		q[primaryAxis] = (float) p1;
		q[secondaryAxis] = (float) p2;
		q[tertiaryAxis] = (float) p3;
		q.W = (float) p0;

		return q;
    }

    public Vector3 ToEulerAngles(Quaternion quaternion) {
		//From http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/quat_2_euler_paper_ver2-1.pdf
		
		double e = circular ? -1 : +1;
		double p0 = quaternion.W;
		double p1 = quaternion[primaryAxis];
		double p2 = quaternion[secondaryAxis];
		double p3 = quaternion[tertiaryAxis];

		double sp0 = MathExtensions.Sqr(p0);
		double sp1 = MathExtensions.Sqr(p1);
		double sp2 = MathExtensions.Sqr(p2);
		double sp3 = MathExtensions.Sqr(p3);

		double halfSinAngle2 = (p0 * p2 + e * p1 * p3);

		double angle1, angle2, angle3;
		double singularityThreshold = 0.4999;
		if (halfSinAngle2 > +singularityThreshold) {
			angle2 = +Math.PI / 2;
			angle1 = 0;
			angle3 = +2 * e * Math.Atan(p1 / p0);
		} else if (halfSinAngle2 < -singularityThreshold) {
			angle2 = -Math.PI / 2;
			angle1 = 0;
			angle3 = -2 * e * Math.Atan(p1 / p0);
		} else {
			angle2 = Math.Asin(2 * halfSinAngle2);
			angle3 = Math.Atan2(2 * (p0 * p3 - e * p1 * p2), 1 - 2 * (sp2 + sp3));
			angle1 = Math.Atan2(2 * (p0 * p1 - e * p2 * p3), 1 - 2 * (sp1 + sp2));

			//convert to smaller l1-norm of angles where possible
			double halfPi = Math.PI / 2;
			if (angle1 > halfPi && angle3 > halfPi) {
				angle1 -= Math.PI;
				angle3 -= Math.PI;
				if (angle2 > 0) {
					angle2 = Math.PI - angle2;
				} else {
					angle2 = -Math.PI - angle2;
				}
			} else if (angle1 < -halfPi && angle3 < -halfPi) {
				angle1 += Math.PI;
				angle3 += Math.PI;
				if (angle2 > 0) {
					angle2 = Math.PI - angle2;
				} else {
					angle2 = -Math.PI - angle2;
				}
			}
		}
		
		Vector3 angles = default(Vector3);
		angles[primaryAxis] = (float) angle1;
		angles[secondaryAxis] = (float) angle2;
		angles[tertiaryAxis] = (float) angle3;

		return angles;
    }
}