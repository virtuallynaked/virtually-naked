public interface IFigureAnimator {
	ChannelInputs GetFrameInputs(ChannelInputs shapeInputs, FrameUpdateParameters updateParameters, ControlVertexInfo[] previousFrameControlVertexInfos);
}
