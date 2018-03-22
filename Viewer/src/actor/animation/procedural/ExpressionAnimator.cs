using SharpDX;
using System;
using System.Linq;

public class ExpressionAnimator : IProceduralAnimator {
	private static readonly string[] ExpressionChannelNames = {
		"eCTRLConfident?value",
		"eCTRLDesire?value",
		"eCTRLExcitement?value",
		"eCTRLFlirting?value",
		"eCTRLHappy?value",
		"eCTRLPleased?value",
		"eCTRLSarcastic?value",
		"eCTRLSmile?value" };

	private static readonly Random rnd = RandomProvider.Provide();

	private const double MinimumTimeBetweenExpressions = 0f;
	private const double MeanTimeBetweenBlinks = 3f;
	
	private readonly Channel[] expressionChannels; 

	private double expressionStartTime = 0;
	private double expressionDuration = 0;
	
	private Channel currentExpression;
	private Channel nextExpression;

	public ExpressionAnimator(ChannelSystem channelSystem) {
		expressionChannels = ExpressionChannelNames
			.Select(name => channelSystem.ChannelsByName[name])
			.ToArray();

		nextExpression = expressionChannels[rnd.Next(expressionChannels.Length)];
	}
	
	public static double GenerateExpressionDuration() {
		//sample from exponential distribution
		double time = -Math.Log(rnd.NextDouble()) * (MeanTimeBetweenBlinks - MinimumTimeBetweenExpressions) + MinimumTimeBetweenExpressions;
		return time;
	}

	private void PrepareNextExpression(float currentTime) {
		currentExpression = nextExpression;
		while (nextExpression == currentExpression) {
			nextExpression = expressionChannels[rnd.Next(expressionChannels.Length)];
		}

		expressionStartTime = currentTime;

		expressionDuration = GenerateExpressionDuration();
	}

	public void Update(FrameUpdateParameters updateParameters, ChannelInputs inputs) {
		double elapsedSinceStart = updateParameters.Time - expressionStartTime;
		double expressionProgress = expressionDuration == 0 ? Double.PositiveInfinity : elapsedSinceStart / expressionDuration;

		if (expressionProgress >= 1) {
			PrepareNextExpression(updateParameters.Time);
			expressionProgress = 0;
		}
		
		foreach (var channel in expressionChannels) {
			channel.SetValue(inputs, 0);
		}
		
		float channelValue = MathUtil.SmoothStep((float) expressionProgress);
		currentExpression.SetValue(inputs, 1 - channelValue);
		nextExpression.SetValue(inputs, channelValue);
	}
}
