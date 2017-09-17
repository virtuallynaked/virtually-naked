public class ToneMappingSettings {
	public const double NeutralWhiteBalance = 6500;
	public const double NeutralExposure = 13;

	public double WhiteBalance { get; set; } = NeutralWhiteBalance;
	public double ExposureValue { get; set; } = NeutralExposure;
	public double BurnHighlights { get; set; } = 0.25;
	public double CrushBlacks { get; set; } = 0.2;
}
