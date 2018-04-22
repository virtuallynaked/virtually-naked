using System.Collections.Immutable;
using System.IO;
using System.Linq;

public class ContentPackImportConfiguration {
	public class Figure {
		public string Name { get; }
		public DirectoryInfo Directory { get; }

		public Figure(string name, DirectoryInfo directory) {
			Name = name;
			Directory = directory;
		}

		public override string ToString() {
			return $"ContentPackImportConfiguration.Figure[{Name}]";
		}
	}

	public class Outfit {
		public string Name { get; }
		public FileInfo File { get; }

		public Outfit(string name, FileInfo file) {
			Name = name;
			File = file;
		}

		public override string ToString() {
			return $"ContentPackImportConfiguration.Outfit[{Name}]";
		}
	}

	public const string CoreName = "core";

	public string Name { get; }
	public ImmutableList<Figure> Figures { get; }
	public ImmutableList<Outfit> Outfits { get; }
	
	public ContentPackImportConfiguration(string name, ImmutableList<Figure> figures, ImmutableList<Outfit> outfits) {
		Name = name;
		Figures = figures;
		Outfits = outfits;
	}
	
	public override string ToString() {
		return $"ContentPackImportConfiguration[{Name}]";
	}

	public bool IsCore => Name == CoreName;

	public static ContentPackImportConfiguration Load(DirectoryInfo confDir) {
		var name = confDir.Name;

		var figuresDir = confDir.Subdirectory("figures");
		var figures = (figuresDir.Exists ? figuresDir.GetDirectories() : new DirectoryInfo[0])
			.Select(figureDir => new Figure(figureDir.Name, figureDir))
			.ToImmutableList();

		var outfitsDir = confDir.Subdirectory("outfits");
		var outfits = (outfitsDir.Exists ? outfitsDir.GetFiles() : new FileInfo[0])
			.Select(outfitFile => new Outfit(outfitFile.Name, outfitFile))
			.ToImmutableList();

		return new ContentPackImportConfiguration(name, figures, outfits);
	}

	public static ImmutableList<ContentPackImportConfiguration> LoadAll(DirectoryInfo confsDir) {
		return confsDir.GetDirectories()
			.Select(confDir => Load(confDir))
			.ToImmutableList();
	}
}
