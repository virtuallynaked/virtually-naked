using System;
using System.Linq;

public enum RenderingLayer {
	OneSidedOpaque, OneSidedBackToFrontTransparent,
	TwoSidedOpaque, TwoSidedBackToFrontTransparent,
	UnorderedTransparent,
	UiElements
}

public enum OutputMode {
	Standard,
	WeightedBlendedOrderIndependent,
	FalseDepth
}

public struct RenderingPass {
	public static readonly RenderingLayer[] Layers = Enum.GetValues(typeof(RenderingLayer)).Cast<RenderingLayer>().ToArray();

	public RenderingLayer Layer { get; }
	public OutputMode OutputMode { get; }

	public RenderingPass(RenderingLayer layer, OutputMode outputMode) {
		Layer = layer;
		OutputMode = outputMode;
	}
}
