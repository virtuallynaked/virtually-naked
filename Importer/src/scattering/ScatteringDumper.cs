using System;
using System.IO;
using System.Linq;

public class ScatteringDumper {
	public static void Dump(ImporterPathManager pathManager, Figure figure, IMaterialSettings[] materialSettingsArray, string materialSetName) {
		var surfaceProperties = SurfacePropertiesJson.Load(pathManager, figure);
		if (!surfaceProperties.PrecomputeScattering) {
			return;
		}
		
		new ScatteringDumper(pathManager, figure, materialSettingsArray, materialSetName).Dump();
	}

	private readonly Figure figure;
	private readonly IMaterialSettings[] materialSettingsArray;
	private readonly DirectoryInfo targetDirectory;

	private ScatteringDumper(ImporterPathManager pathManager, Figure figure, IMaterialSettings[] materialSettingsArray, string materialSetName) {
		this.figure = figure;
		this.materialSettingsArray = materialSettingsArray;

		targetDirectory = pathManager.GetDestDirForFigure(figure.Name)
			.Subdirectory("scattering")
			.Subdirectory(materialSetName);
	}
	
	private PackedLists<WeightedIndex> CalculateFormFactorsForColorComponent(
		ScatteringFormFactorCalculator formFactorCalculator,
		IMaterialSettings[] materialSettingsArray, int componentIdx) {
		var profiles = materialSettingsArray
			.Select(baseMaterialSettings => {
				UberMaterialSettings materialSettings = baseMaterialSettings as UberMaterialSettings;
				if (materialSettings is null) {
					return null;
				}

				if (materialSettings.thinWalled) {
					return null;
				}

				var volumeSettings = new VolumeParameters(
					materialSettings.transmittedMeasurementDistance,
					materialSettings.transmittedColor[componentIdx],
					materialSettings.scatteringMeasurementDistance,
					materialSettings.sssAmount,
					materialSettings.sssDirection);
				ScatteringProfile profile = new ScatteringProfile(
					volumeSettings.SurfaceAlbedo,
					volumeSettings.MeanFreePathLength);
				return profile;
			})
			.ToArray();
		
		PackedLists<WeightedIndex> formFactors = formFactorCalculator.Calculate(profiles);
		return formFactors;
	}

	private void Dump() {
		FileInfo formFactorSegmentsFile = targetDirectory.File(Scatterer.FormFactorSegmentsFilename);
		FileInfo formFactorElementsFile = targetDirectory.File(Scatterer.FormFactoryElementsFilename);
		if (formFactorElementsFile.Exists && formFactorSegmentsFile.Exists) {
			return;
		}
		Console.WriteLine("Dumping scattering form-factors...");

		ScatteringFormFactorCalculator formFactorCalculator = ScatteringFormFactorCalculator.Make(figure.Geometry);
		
		//Note on units:
		// Daz uses units of cm for its "distance of measurement" volume parameters
		// Therefore, the calculted MeanFreePathLength is also in cm
		// And therefore, the radius and distance values passed to the scattering calculator must also be in cm

		PackedLists<WeightedIndex> redFormFactors = CalculateFormFactorsForColorComponent(formFactorCalculator, materialSettingsArray, 0);
		PackedLists<WeightedIndex> greenFormFactors = CalculateFormFactorsForColorComponent(formFactorCalculator, materialSettingsArray, 1);
		PackedLists<WeightedIndex> blueFormFactors = CalculateFormFactorsForColorComponent(formFactorCalculator, materialSettingsArray, 2);
		
		var formFactors = Vector3WeightedIndex.Merge(redFormFactors, greenFormFactors, blueFormFactors);
		
		targetDirectory.CreateWithParents();
		formFactorSegmentsFile.WriteArray(formFactors.Segments);
		formFactorElementsFile.WriteArray(formFactors.Elems);
	}
}
