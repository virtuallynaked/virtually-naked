using System;
using System.Collections.Generic;
using Valve.VR;

public class FrameTimingMonitor {
	private const int ReportRate = 50;
	private const int QueueCapacity = 1000;

	private int frameCount = 0;
	private readonly Queue<float> timingsQueue = new Queue<float>(QueueCapacity);
	private double totalInQueue = 0;
	
	public void Update() {
		frameCount += 1;

		var frameTiming = OpenVR.Compositor.GetFrameTiming(1);
		var time = frameTiming.m_flTotalRenderGpuMs;

		while (timingsQueue.Count >= QueueCapacity) {
			float removed = timingsQueue.Dequeue();
			totalInQueue -= removed;
		}

		totalInQueue += time;
		timingsQueue.Enqueue(time);

		double meanInQueue = totalInQueue / timingsQueue.Count;

		if (frameCount % ReportRate == 0) {
			Console.WriteLine("frame GPU time = " + meanInQueue);
		}
	}
}
