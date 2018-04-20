public class MultiMaterialSettings {
	public class Variant {
		public string Name { get; }
		public IMaterialSettings[] SettingsBySurface { get; }

		public Variant(string name, IMaterialSettings[] settingsBySurface) {
			Name = name;
			SettingsBySurface = settingsBySurface;
		}
	}

	public class VariantCategory {
		public string Name { get; }
		public int[] Surfaces { get; }
		public Variant[] Variants { get; }

		public VariantCategory(string name, int[] surfaces, Variant[] variants) {
			Name = name;
			Surfaces = surfaces;
			Variants = variants;
		}
	}

	public IMaterialSettings[] PerMaterialSettings { get; }
	public VariantCategory[] VariantCategories { get; }

	public MultiMaterialSettings(IMaterialSettings[] perMaterialSettings, VariantCategory[] variantCategories) {
		PerMaterialSettings = perMaterialSettings;
		VariantCategories = variantCategories ?? new VariantCategory[0];
	}
}
