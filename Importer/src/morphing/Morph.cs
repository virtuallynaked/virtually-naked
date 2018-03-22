using SharpDX;

public class Morph {
	private readonly Channel channel;
	private readonly MorphDelta[] deltas;

	public Morph(Channel channel, MorphDelta[] deltas) {
		this.channel = channel;
		this.deltas = deltas;
	}
	
	public string Name => channel.Name;
	public Channel Channel => channel;
	public MorphDelta[] Deltas => deltas;

	public void Apply(ChannelOutputs channelOutputs, Vector3[] vertices) {
		float weight = (float) channel.GetValue(channelOutputs);

		//Console.WriteLine("Applying " + channel.Name + " at " + weight + "...");
		
		if (weight == 0) {
			return;
		}
		
		foreach (var delta in deltas) {
			vertices[delta.VertexIdx] += weight * delta.PositionOffset;
		}
	}
}
