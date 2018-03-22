public class RenderingConstants {
	//near and far are intentionally reversed to enchance z-buffer precision (the "reverse-z trick")
	public const float ZNear = 100f;
	public const float ZFar = 0.01f;

	public const float DepthClearValue = 0;
}
