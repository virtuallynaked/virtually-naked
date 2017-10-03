using System;
using System.Collections.Generic;

public class GenericRangeMenuItem : IRangeMenuItem {
	public string Label { get; }
	public double Min { get; }
	public double Max { get; }

	private double defaultValue;
	private Func<double> getValue;
	private Action<double> setValue;

	public GenericRangeMenuItem(string label, double min, double defaultValue, double max, Func<double> getValue, Action<double> setValue) {
		Label = label;
		Min = min;
		Max = max;

		this.defaultValue = defaultValue;
		this.getValue = getValue;
		this.setValue = setValue;
	}
	
	public double Value => getValue();

	public void ResetValue() {
		setValue(defaultValue);
	}

	public void SetValue(double value) {
		setValue(value);
	}
}

public class WhiteBalanceMenuItem : IRangeMenuItem {
	public string Label => "White Balance";
	public double Min => -1;
	public double Max => +2;

	private ToneMappingSettings settings;

	public WhiteBalanceMenuItem(ToneMappingSettings settings) {
		this.settings = settings;
	}
	
	public double Value => Math.Log(settings.WhiteBalance / ToneMappingSettings.NeutralWhiteBalance);

	public void ResetValue() {
		settings.WhiteBalance = ToneMappingSettings.NeutralWhiteBalance;
	}

	public void SetValue(double value) {
		settings.WhiteBalance = Math.Exp(value) * ToneMappingSettings.NeutralWhiteBalance;
	}
}


public class ExposureMenuItem : IRangeMenuItem {
	public string Label => "Exposure";
	public double Min => -5;
	public double Max => +5;

	private ToneMappingSettings settings;

	public ExposureMenuItem(ToneMappingSettings settings) {
		this.settings = settings;
	}
	
	public double Value => settings.ExposureValue - ToneMappingSettings.NeutralExposure;

	public void ResetValue() {
		settings.ExposureValue = ToneMappingSettings.NeutralExposure;
	}

	public void SetValue(double value) {
		settings.ExposureValue = ToneMappingSettings.NeutralExposure + value;
	}
}

public class ToneMappingMenuLevel : IMenuLevel {
	private List<IMenuItem> items;
	
	public ToneMappingMenuLevel(ToneMappingSettings settings) {
		items = new List<IMenuItem> {
			new WhiteBalanceMenuItem(settings),
			new ExposureMenuItem(settings),
			new GenericRangeMenuItem("Burn Highlights", 0, 0.25, 1, () => settings.BurnHighlights, (value) => settings.BurnHighlights = value),
			new GenericRangeMenuItem("Crush Shadows", 0, 0.2, 1, () => settings.CrushBlacks, (value) => settings.CrushBlacks = value)
		};
	}

	public event Action ItemsChanged {
		add { }
		remove { }
	}

	public List<IMenuItem> GetItems() {
		return items;
	}
}
