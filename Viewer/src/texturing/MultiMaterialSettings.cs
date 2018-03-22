public class MultiMaterialSettings {
	public IMaterialSettings[] PerMaterialSettings { get; }

	public MultiMaterialSettings(IMaterialSettings[] perMaterialSettings) {
		PerMaterialSettings = perMaterialSettings;
	}
}
