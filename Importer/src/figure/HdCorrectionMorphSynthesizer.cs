using OpenSubdivFacade;
using SharpDX;
using System.Linq;

public class HdCorrectionMorphSynthesizer {
	private readonly string figureName;
	private readonly Geometry geometry;
	private readonly Subdivider limit0Subdivider;

	public HdCorrectionMorphSynthesizer(string figureName, Geometry geometry) {
		this.figureName = figureName;
		this.geometry = geometry;
		limit0Subdivider = new Subdivider(geometry.MakeStencils(StencilKind.LimitStencils, 0));
	}

	private string ChannelName => figureName + "-hd-correction?value";
	
	public ChannelRecipe SynthesizeChannel(double initialValue = 0) {
		return new ChannelRecipe {
			Name = ChannelName,
			InitialValue = initialValue,
			Min = 0,
			Max = 1,
			Clamped = true
		};
	}

	public MorphRecipe SynthesizeMorph() {
		Vector3[] controlVertices = geometry.VertexPositions;
		Vector3[] refinedVertices = limit0Subdivider.Refine(controlVertices, new Vector3Operators());
		MorphDelta[] deltas = Enumerable.Range(0, controlVertices.Length)
			.Select(i => new MorphDelta(i, controlVertices[i] - refinedVertices[i]))
			.ToArray();

		return new MorphRecipe {
			Channel = ChannelName,
			Deltas = deltas
		};
	}
}
