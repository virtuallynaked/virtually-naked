using System;

public class ExponentiallyWeightedMovingAverager<T, U> where U : struct, IVectorOperators<T>  {
	private static readonly U operators = new U();

	private readonly float meanLifetime;

	private float prevTime = 0;
	private T meanValue;
	
	public ExponentiallyWeightedMovingAverager(float meanLifetime, T initialValue) {
		this.meanLifetime = meanLifetime;
		this.meanValue = initialValue;
	}
	
	public T MeanValue => meanValue;
	
	public void Update(float time, T value) {
		float deltaTime = time - prevTime;
		prevTime = time;

		float decay = (float) Math.Exp(-deltaTime / meanLifetime);
		meanValue = operators.Add(
			operators.Mul(decay, meanValue),
			operators.Mul(1 - decay, value));
	}
}