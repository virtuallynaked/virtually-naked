using System.Collections.Immutable;
using System.Linq;

public class MaterialSetAndVariantOption {
	public static MaterialSetAndVariantOption MakeWithDefaultVariants(MaterialSetOption materialSet) {
		var variantSelections = materialSet.Settings.VariantCategories
			.ToImmutableDictionary(category => category.Name, category => category.Variants[0].Name);
		return new MaterialSetAndVariantOption(materialSet, variantSelections);
	}

	public MaterialSetOption MaterialSet { get; }
	public ImmutableDictionary<string, string> VariantSelections { get; }

	public MaterialSetAndVariantOption(MaterialSetOption option, ImmutableDictionary<string, string> variantSelection) {
		MaterialSet = option;
		VariantSelections = variantSelection;
	}
	
	public IMaterialSettings[] PerSurfaceSettings {
		get {
			var materialSettingsBySurface = (IMaterialSettings[]) MaterialSet.Settings.PerMaterialSettings.Clone();

			foreach (var variantCategory in MaterialSet.Settings.VariantCategories) {
				if (!VariantSelections.TryGetValue(variantCategory.Name, out string variantSelection)) {
					continue;
				}

				var variant = variantCategory.Variants
					.Where(variantOption => variantOption.Name == variantSelection)
					.FirstOrDefault();
				if (variant == null) {
					continue;
				}

				for (int variantSurfaceIdx = 0; variantSurfaceIdx < variantCategory.Surfaces.Length; ++variantSurfaceIdx) {
					int surfaceIdx = variantCategory.Surfaces[variantSurfaceIdx];
					var materialSettings = variant.SettingsBySurface[variantSurfaceIdx];
					materialSettingsBySurface[surfaceIdx] = materialSettings;
				}
			}

			return materialSettingsBySurface;
		}
	}

	public MaterialSetAndVariantOption SetVariant(MultiMaterialSettings.VariantCategory category, MultiMaterialSettings.Variant variant) {
		return new MaterialSetAndVariantOption(MaterialSet, VariantSelections.SetItem(category.Name, variant.Name));
	}
}
