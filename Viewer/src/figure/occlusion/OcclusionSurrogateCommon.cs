public class OcclusionSurrogateCommon {
	public const int SubdivisionLevel = 4; //NB: this must match the number of subdivide calls in OcclusionSurrogate.hlsl
	public static readonly TriMesh Mesh = GeometricPrimitiveFactory.MakeOctahemisphere(SubdivisionLevel);
}
