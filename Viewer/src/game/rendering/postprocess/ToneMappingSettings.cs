using Newtonsoft.Json;
using System;

public class ToneMappingSettings {
	public const double NeutralWhiteBalance = 6500;
	public const double NeutralExposure = 13;
	
	public double WhiteBalance { get; set; } = NeutralWhiteBalance;
	public double ExposureValue { get; set; } = NeutralExposure;
	public double BurnHighlights { get; set; } = 0.25;
	public double CrushBlacks { get; set; } = 0.2;


	public class Recipe {
		[JsonProperty("white-balance")]
		public double? whiteBalance;

		[JsonProperty("exposure-value")]
		public double? exposureValue;

		[JsonProperty("burn-highlights")]
		public double? burnHighlights;

		[JsonProperty("crush-shadows")]
		public double? crushBlacks;

		public void Merge(ToneMappingSettings settings) {
			settings.WhiteBalance = whiteBalance.GetValueOrDefault(settings.WhiteBalance);
			settings.ExposureValue = exposureValue.GetValueOrDefault(settings.ExposureValue);
			settings.BurnHighlights = burnHighlights.GetValueOrDefault(settings.BurnHighlights);
			settings.CrushBlacks = crushBlacks.GetValueOrDefault(settings.CrushBlacks);
		}
	}

	public Recipe Recipize() {
		return new Recipe {
			whiteBalance = WhiteBalance,
			exposureValue = ExposureValue,
			burnHighlights = BurnHighlights,
			crushBlacks = CrushBlacks
		};
	}
}
