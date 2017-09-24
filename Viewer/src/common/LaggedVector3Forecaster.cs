using SharpDX;

class LaggedVector3Forecaster {
	private readonly float lagAmount = 0.04f;
	private Vector3 laggedValue = Vector3.Zero;
	private Vector3 twiceLaggedValue = Vector3.Zero;

	public LaggedVector3Forecaster(float lagAmount, Vector3 initialValue) {
		this.lagAmount = lagAmount;
		this.laggedValue = initialValue;
		this.twiceLaggedValue = initialValue;
	}

	public LaggedVector3Forecaster(float lagAmount) : this(lagAmount, Vector3.Zero) {
	}

	public void Update(Vector3 value) {
		laggedValue = Vector3.Lerp(laggedValue, value, lagAmount);
		twiceLaggedValue = Vector3.Lerp(twiceLaggedValue, value, lagAmount / 2);
	}

	public Vector3 ForecastValue {
		get {
			return 2 * laggedValue - twiceLaggedValue;
		}
	}
}
