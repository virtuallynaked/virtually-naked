using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;

class OccluderChannelPicker : IOperationVisitor {
	private ChannelSystem channelSystem;
	private bool[] isFormulaInput;
	
	public OccluderChannelPicker(ChannelSystem channelSystem) {
		this.channelSystem = channelSystem;

		isFormulaInput = new bool[channelSystem.Channels.Count];

		foreach (Channel channel in channelSystem.Channels) {
			TagInputs(channel);
		}
	}

	private static readonly string[] PotentialPathPrefixes = new string[] {
		"/Shapes/",
		"/Pose Controls/Head/Expressions/",
		"/Pose Controls/Head/Visemes/" };

	private bool IsPotentialSource(Channel channel) {
		if (!channel.Visible) {
			return false;
		}

		foreach (string prefix in PotentialPathPrefixes) {
			if (channel.Path.StartsWith(prefix)) {
				return true;
			}
		}

		return false;
	}

	private void TagInputs(Channel channel) {
		if (!IsPotentialSource(channel)) {
			return;
		}

		foreach (Formula formula in channel.SumFormulas) {
			formula.Accept(this);
		}
		foreach (Formula formula in channel.MultiplyFormulas) {
			formula.Accept(this);
		}
	}

	public List<Channel> GetChannels() {
		return channelSystem.Channels
			.Where(channel => IsPotentialSource(channel) && !isFormulaInput[channel.Index])
			.ToList();
	}

	void IOperationVisitor.Add() {
	}

	void IOperationVisitor.Div() {
	}

	void IOperationVisitor.Mul() {
	}

	void IOperationVisitor.PushChannel(Channel channel) {
		if (isFormulaInput[channel.Index]) {
			return;
		}
		isFormulaInput[channel.Index] = true;
		TagInputs(channel);
	}

	void IOperationVisitor.PushValue(double value) {
	}

	void IOperationVisitor.Spline(Spline spline) {
	}

	void IOperationVisitor.Sub() {
	}
}

public class OccluderParametersCalculator : IDisposable {
	private const double OcclusionDifferenceThreshold = 1 / 256d;
	
	private Figure figure;
	private ChannelInputs shapeInputs;
	private FigureGroup figureGroup;
	private FigureOcclusionCalculator occlusionCalculator;
	
	public OccluderParametersCalculator(ContentFileLocator fileLocator, Device device, ShaderCache shaderCache, Figure figure, float[] faceTransparencies, ChannelInputs shapeInputs) {
		this.figure = figure;
		this.shapeInputs = shapeInputs;
		figureGroup = new FigureGroup(figure);
		var faceTransparenciesGroup = new FaceTransparenciesGroup(faceTransparencies);
		occlusionCalculator = new FigureOcclusionCalculator(fileLocator, device, shaderCache, figureGroup, faceTransparenciesGroup);
	}

	public void Dispose() {
		occlusionCalculator.Dispose();
	}

	private IEnumerable<Channel> GetChannelsForOcclusionSystem() {
		ChannelSystem channelSystem = figureGroup.Parent.ChannelSystem;
		return new OccluderChannelPicker(channelSystem).GetChannels();
	}
	
	private ChannelInputs MakePosedShapeInputs() {
		var inputs = new ChannelInputs(shapeInputs);
		figure.ChannelsByName["pCTRLArmsUpDwn?value"].SetValue(inputs, -1/4d);
		figure.ChannelsByName["pCTRLArmsFrntBck?value"].SetValue(inputs, 1/4d);
		figure.ChannelsByName["pCTRLKneesUp?value"].SetValue(inputs, 1/4d);
		figure.ChannelsByName["pCTRLLegsOut?value"].SetValue(inputs, 1/4d);
		return inputs;
	}
	
	private OcclusionInfo[] CalculateOcclusion(ChannelOutputs outputs) {
		var outputsGroup = new ChannelOutputsGroup(outputs, new ChannelOutputs[0]);
		return occlusionCalculator.CalculateOcclusionInformation(outputsGroup).ParentOcclusion;
	}

	public OccluderParameters CalculateOccluderParameters() {
		var baseInputs = MakePosedShapeInputs();
		var baseOutputs = figure.Evaluate(null, baseInputs);
		var baseOcclusionInfos = CalculateOcclusion(baseOutputs);
		
		List<Channel> channels = new List<Channel>();
		List<List<OcclusionDelta>> perVertexDeltas = new List<List<OcclusionDelta>>();
		for (int i = 0; i < baseOcclusionInfos.Length; ++i) {
			perVertexDeltas.Add(new List<OcclusionDelta>());
		}
		
		foreach (var channel in GetChannelsForOcclusionSystem()) {
			Console.WriteLine($"\t{channel.Name}...");
			
			int occlusionChannelIdx = channels.Count;
			channels.Add(channel);

			var inputs = new ChannelInputs(baseInputs);
			channel.SetValue(inputs, 1);
			var outputs = figure.Evaluate(null, inputs);
			var occlusionInfos = CalculateOcclusion(outputs);
			
			for (int vertexIdx = 0; vertexIdx < occlusionInfos.Length; ++vertexIdx) {
				if (Math.Abs(occlusionInfos[vertexIdx].Front - baseOcclusionInfos[vertexIdx].Front) > OcclusionDifferenceThreshold) {
					var delta = new OcclusionDelta(occlusionChannelIdx, OcclusionInfo.Pack(occlusionInfos[vertexIdx]));
					perVertexDeltas[vertexIdx].Add(delta);
				}
			}
		}

		var parameters = new OccluderParameters(
			OcclusionInfo.PackArray(baseOcclusionInfos),
			channels.Select(channel => channel.Name).ToList(),
			PackedLists<OcclusionDelta>.Pack(perVertexDeltas));

		return parameters;
	}
}
