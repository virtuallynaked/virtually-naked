using SharpDX;
using static System.Math;

public struct Swing {
	//twist axis constants
	public const int XAxis = 0;
	public const int YAxis = 1;
	public const int ZAxis = 2;

	private readonly float x;
	private readonly float y;
	
	public Swing(float x, float y) {
		this.x = x;
		this.y = y;
	}

	public float X => x;
	public float Y => y;

	public Quaternion AsQuaternion(int twistAxis) {
		Vector3 axis = default(Vector3);
		axis[(twistAxis + 1) % 3] = x;
		axis[(twistAxis + 2) % 3] = y;

		float angle = (float) Sqrt(x * x + y * y);

		return Quaternion.RotationAxis(axis, angle);
	}
	
	public static Swing FromTo(int twistAxis, Vector3 from, Vector3 to) {
		DebugUtilities.AssertIsUnit(from);
		DebugUtilities.AssertIsUnit(to);

		float fromX = from[(twistAxis + 1) % 3];
		float fromY = from[(twistAxis + 2) % 3];
		float fromZ = from[(twistAxis + 0) % 3];

		float toX = to[(twistAxis + 1) % 3];
		float toY = to[(twistAxis + 2) % 3];
		float toZ = to[(twistAxis + 0) % 3];

		Vector2 axis = Vector2.Normalize(new Vector2(fromY - toY, toX - fromX));
		
		float projectionLength = axis.X * fromX + axis.Y * fromY; 
		Vector2 projection = axis * projectionLength; //by construction, projection onto axis is same for from and to
		Vector3 fromRejection = new Vector3(fromX - projection.X, fromY - projection.Y, fromZ);
		Vector3 toRejection = new Vector3(toX - projection.X, toY - projection.Y, toZ);
		Vector3 rejectionCross = Vector3.Cross(fromRejection, toRejection);
		float rejectionDot = Vector3.Dot(fromRejection, toRejection);
				
		float angle = (float) Atan2(axis.X * rejectionCross.X + axis.Y * rejectionCross.Y, rejectionDot);
		
		return new Swing(angle * axis.X, angle * axis.Y);
	}
}