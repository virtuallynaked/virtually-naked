using Phonon;
using SharpDX;
using SharpDX.XAudio2;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

public class PhononSourceVoiceStream : Stream {
	private const int FrameSize = 1024;
	private SourceVoice voice;
	
	private short[] frameBuffer = new short[FrameSize];
	private int frameBufferPosition = 0;

	private IntPtr phononEffect;

	private float[] phononInputArray;
	private GCHandle phononInputArrayHandle;
	private Phonon.AudioBuffer phononInputBuffer;
	
	private float[] phononOutputArray;
	private GCHandle phononOutputArrayHandle;
	private Phonon.AudioBuffer phononOutputBuffer;

	public SharpDX.Vector3 HeadRelativePosition { get; set; }

	public PhononSourceVoiceStream(SourceVoice voice) {
		this.voice = voice;
		
		GlobalContext globalContext = default(GlobalContext);

		RenderingSettings renderingSettings = default(RenderingSettings);
		renderingSettings.samplingRate = 44100;
		renderingSettings.frameSize = FrameSize;
		renderingSettings.convolutionOption = ConvolutionOption.Phonon;

		HRTFParams hrtfParams = default(HRTFParams);
		hrtfParams.type = HRTFDatabaseType.Default;
		
		IntPtr phononRenderer = IntPtr.Zero;
		PhononCore.iplCreateBinauralRenderer(globalContext, renderingSettings, hrtfParams, ref phononRenderer);

		AudioFormat inputFormat = default(AudioFormat);
		inputFormat.channelLayoutType = ChannelLayoutType.Speakers;
		inputFormat.channelLayout = ChannelLayout.Mono;
		inputFormat.channelOrder = ChannelOrder.Interleaved;

		AudioFormat outputFormat = default(AudioFormat);
		outputFormat.channelLayoutType = ChannelLayoutType.Speakers;
		outputFormat.channelLayout = ChannelLayout.Stereo;
		outputFormat.channelOrder = ChannelOrder.Interleaved;

		phononInputArray = new float[FrameSize];
		phononInputArrayHandle = GCHandle.Alloc(phononInputArray, GCHandleType.Pinned);
		phononInputBuffer = default(Phonon.AudioBuffer);
		phononInputBuffer.audioFormat = inputFormat;
		phononInputBuffer.numSamples = FrameSize;
		phononInputBuffer.interleavedBuffer = phononInputArrayHandle.AddrOfPinnedObject();

		phononOutputArray = new float[FrameSize * 2];
		phononOutputArrayHandle = GCHandle.Alloc(phononOutputArray, GCHandleType.Pinned);
		phononOutputBuffer = default(Phonon.AudioBuffer);
		phononOutputBuffer.audioFormat = outputFormat;
		phononOutputBuffer.numSamples = FrameSize;
		phononOutputBuffer.interleavedBuffer = phononOutputArrayHandle.AddrOfPinnedObject();

		phononEffect = IntPtr.Zero;
		PhononCore.iplCreateBinauralEffect(phononRenderer, inputFormat, outputFormat, ref phononEffect);
	}

	public override bool CanRead => throw new NotImplementedException("CanRead");

	public override bool CanSeek => throw new NotImplementedException("CanSeek");

	public override bool CanWrite => throw new NotImplementedException("CanWrite");

	public override long Length => throw new NotImplementedException("Length");

	public override long Position { get => 0; set => throw new NotImplementedException("set_Position"); }

	public override void Flush() {
		throw new NotImplementedException("Flush");
	}

	public override int Read(byte[] buffer, int offset, int count) {
		throw new NotImplementedException("Read");
	}

	public override long Seek(long offset, SeekOrigin origin) {
		throw new NotImplementedException("Seek");
	}

	public override void SetLength(long value) {
		throw new NotImplementedException("SetLength");
	}

	private void SubmitFrameBuffer() {
		//Console.WriteLine(HeadRelativePosition);
		
		float amplification = 0.5f / HeadRelativePosition.LengthSquared();

		for (int i = 0; i < FrameSize; ++i) {
			phononInputArray[i] = (float) frameBuffer[i] / short.MaxValue;
			phononInputArray[i] *= amplification;
		}
		
		Phonon.Vector3 direction = new Phonon.Vector3 {
			x = HeadRelativePosition.X,
			y = HeadRelativePosition.Y,
			z = HeadRelativePosition.Z - 1e-9f
		};
		PhononCore.iplApplyBinauralEffect(
			phononEffect,
			phononInputBuffer,
			direction,
			HRTFInterpolation.Bilinear,
			phononOutputBuffer);
		
		var dataStream = new DataStream(FrameSize * sizeof(float) * 2, true, true);
		dataStream.WriteRange(phononOutputArray);
		dataStream.Seek(0, SeekOrigin.Begin);
		var audioBuffer = new SharpDX.XAudio2.AudioBuffer(dataStream);
		voice.SubmitSourceBuffer(audioBuffer, null);
	}

	public override void Write(byte[] byteBuffer, int offset, int count) {
		while (count > 0) {
			int bufferRemaining = FrameSize - frameBufferPosition;

			int copyAmount = Math.Min(bufferRemaining * sizeof(short), count);
			Buffer.BlockCopy(byteBuffer, offset, frameBuffer, frameBufferPosition * sizeof(short), copyAmount);
			offset += copyAmount;
			count -= copyAmount;
			frameBufferPosition += copyAmount / sizeof(short);

			while (voice.State.BuffersQueued > 1) {
				Thread.Sleep(1);
			}

			if (frameBufferPosition == FrameSize) {
				SubmitFrameBuffer();
				frameBufferPosition = 0;
			}
		}
		
	}
}
