using System;

public class BlinkAnimator : IProceduralAnimator {
	private static readonly Random rnd = RandomProvider.Provide();

	private const double MinimumTimeBetweenBlinks = 1.5f;
	private const double MeanTimeBetweenBlinks = 4f;
	private const double CloseDuration = 20/300d;
	private const double OpenHalflife = 30/300d;
	private const double MaximumCloseAmount = 1.25;
	
	private readonly Channel eyesClosedChannel;

	private double lastTime = 0;
	private double eyesClosedAmount = 0;
	private bool blinking = false;
	private double timeUntilNextBlink = GenerateTimeUntilNextBlink();

	public BlinkAnimator(ChannelSystem channelSystem) {
		eyesClosedChannel = channelSystem.ChannelsByName["eCTRLEyesClosed?value"];
	}
	
	public static double GenerateTimeUntilNextBlink() {
		//sample from exponential distribution
		double time = -Math.Log(rnd.NextDouble()) * (MeanTimeBetweenBlinks - MinimumTimeBetweenBlinks) + MinimumTimeBetweenBlinks;
		return time;
	}

	public void Update(FrameUpdateParameters updateParameters, ChannelInputs inputs) {
		double elapsed = Math.Max(updateParameters.Time - lastTime, 0);
		lastTime = updateParameters.Time;

		if (blinking) {
			eyesClosedAmount += elapsed / CloseDuration;
			if (eyesClosedAmount < MaximumCloseAmount) {
				elapsed = 0;
			} else {
				//set elapsed to time remaining after close completion
				elapsed = (eyesClosedAmount - 1) * CloseDuration; //set elapsed to time as 
				eyesClosedAmount = 1;
				blinking = false;
				timeUntilNextBlink = GenerateTimeUntilNextBlink();
			}
		}

		if (!blinking) {
			eyesClosedAmount *= Math.Pow(0.5, elapsed / OpenHalflife);

			timeUntilNextBlink -= elapsed;
			if (timeUntilNextBlink < 0) {
				blinking = true;
			}
		}

		eyesClosedChannel.SetValue(inputs, MaximumCloseAmount * eyesClosedAmount);
	}
}
