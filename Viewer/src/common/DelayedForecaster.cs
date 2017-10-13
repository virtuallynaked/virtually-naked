using System.Collections.Generic;

public class DelayedForecaster<T, U> where U : struct, IVectorOperators<T>  {
	private static readonly U operators = new U();

	private struct Record {
		public readonly float timestamp;
		public readonly T position;
		public readonly T velocity;

		public Record(float timestamp, T position, T velocity) {
			this.timestamp = timestamp;
			this.position = position;
			this.velocity = velocity;
		}
	}

	private readonly float delayTime;
	private readonly ExponentiallyWeightedMovingAverager<T, U> velocityAverager;
	private readonly Queue<Record> queue = new Queue<Record>();
	
	private float prevTime;
	private T prevPosition;

	private Record delayedRecord;
	
	public DelayedForecaster(float delayTime, float velocityEstimationTimeConstant, T initialPosition) {
		this.delayTime = delayTime;
		velocityAverager = new ExponentiallyWeightedMovingAverager<T, U>(velocityEstimationTimeConstant, operators.Zero());
		delayedRecord = new Record(0, initialPosition, operators.Zero());
	}

	public T Forecast {
		get {
			float timeSinceRecord = prevTime - delayedRecord.timestamp;
			return operators.Add(
				delayedRecord.position,
				operators.Mul(timeSinceRecord, delayedRecord.velocity));
		}
	}

	public void Update(float time, T position) {
		float deltaTime = time - this.prevTime;
		T deltaPosition = operators.Add(position, operators.Mul(-1, prevPosition));

		prevTime = time;
		prevPosition = position;

		if (deltaTime > 0) {
			T velocity = operators.Mul(1 / deltaTime, deltaPosition);
			velocityAverager.Update(time, velocity);
		}
		
		queue.Enqueue(new Record(time, position, velocityAverager.MeanValue));
		
		float timeToKeep = time - delayTime;
		while (queue.Count > 0 && queue.Peek().timestamp < timeToKeep) {
			delayedRecord = queue.Dequeue();
		}
	}
}