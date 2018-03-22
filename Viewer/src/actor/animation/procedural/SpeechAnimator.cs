using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.IO;
using System.Linq;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using Valve.VR;
using Vector3 = SharpDX.Vector3;

public class SpeechAnimator : IProceduralAnimator {
	public const string Voice =
		//"Microsoft Server Speech Text to Speech Voice (en-CA, Heather)";
		//"Microsoft Server Speech Text to Speech Voice (en-GB, Hazel)";
		//"Microsoft Server Speech Text to Speech Voice (en-IN, Heera)";
		//"Microsoft Server Speech Text to Speech Voice (en-AU, Hayley)";
		//"Microsoft Server Speech Text to Speech Voice (en-US, Helen)";
		//"Microsoft Server Speech Text to Speech Voice (en-US, ZiraPro)"
		//"IVONA 2 Salli OEM"
		"IVONA 2 Raveena OEM";

	private static readonly string Text = new FileInfo("speech/demo.txt").ReadAllText();
	
	private readonly static string[] VisemeChannelMap = new string[] {
			null, //0: silence
			"eCTRLvEH", //1: ae, ax, ah
			"eCTRLvAA", //2: aa
			"eCTRLvOW", //3: ao
			"eCTRLvEH", //4: ey, eh, uh
			"eCTRLvER", //5: er
			"eCTRLvIY", //6: y, iy, ih, ix
			"eCTRLvW", //7: w, uw
			"eCTRLvOW", //8: ow
			"eCTRLvUW", //9: aw
			"eCTRLvOW", //10: oy
			"eCTRLvUW", //11: ay
			"eCTRLvIH", //12: h
			"eCTRLvER", //13: r
			"eCTRLvL", //14: l
			"eCTRLvS", //15: s, z
			"eCTRLvSH", //16: sh, ch, jh, zh
			"eCTRLvTH", //17: th, dh
			"eCTRLvF", //18: f, v
			"eCTRLvT", //19: d, t, n
			"eCTRLvK", //20: k, g, ng
			"eCTRLvM", //21: p, b, m
		};

	private readonly ChannelSystem channelSystem;
	private readonly BoneSystem boneSystem;
	private readonly SpeechSynthesizer synth;
	
	private float currentTime;

	private float visemeStartTime;
	private float visemeDuration;
	private int currentViseme;
	private int nextViseme;

	private Channel[] visemeChannels;

	private MasteringVoice masteringVoice;
	private SourceVoice sourceVoice;

	private readonly Bone headBone;

	private PhononSourceVoiceStream phononStream;

	public SpeechAnimator(ChannelSystem channelSystem, BoneSystem boneSystem) {
		this.channelSystem = channelSystem;
		this.boneSystem = boneSystem;

		this.synth = new SpeechSynthesizer();
		synth.Volume = 100;

		synth.SelectVoice(Voice);
		synth.VisemeReached += Synth_VisemeReached;

		visemeChannels = VisemeChannelMap
			.Select(name => name == null ? null : channelSystem.ChannelsByName[name + "?value"])
			.ToArray();

		headBone = boneSystem.BonesByName["head"];
		
		var audioDevice = new XAudio2(XAudio2Flags.DebugEngine, ProcessorSpecifier.AnyProcessor);
		masteringVoice = new MasteringVoice(audioDevice);
		
		WaveFormat monoWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
		sourceVoice = new SourceVoice(audioDevice, monoWaveFormat, VoiceFlags.None);
		sourceVoice.SetVolume(1);
		sourceVoice.Start();
		
		phononStream = new PhononSourceVoiceStream(sourceVoice);
		synth.SetOutputToAudioStream(phononStream, new SpeechAudioFormatInfo(44100, AudioBitsPerSample.Sixteen, AudioChannel.Mono));
	}

	private void Synth_VisemeReached(object sender, VisemeReachedEventArgs e) {
		visemeStartTime = currentTime;
		visemeDuration = (float) e.Duration.TotalSeconds;
		currentViseme = e.Viseme;
		nextViseme = e.NextViseme;
	}
	
	private void Update3dAudioPosition(FrameUpdateParameters updateParameters, ChannelInputs inputs) {
		TrackedDevicePose_t gamePose = updateParameters.GamePoses[OpenVR.k_unTrackedDeviceIndex_Hmd];
		Matrix hmdToWorldTransform = gamePose.mDeviceToAbsoluteTracking.Convert();
		hmdToWorldTransform.Invert();
		
		var outputs = channelSystem.Evaluate(null, inputs);
		var headTotalTransform = headBone.GetChainedTransform(outputs);

		var headBindPoseCenter = headBone.CenterPoint.GetValue(outputs);
		Vector3 headWorldPosition = headTotalTransform.Transform(headBindPoseCenter) / 100;

		Vector3 headHmdPosition = Vector3.TransformCoordinate(headWorldPosition, hmdToWorldTransform);

		phononStream.HeadRelativePosition = headHmdPosition;
	}

	public void Update(FrameUpdateParameters updateParameters, ChannelInputs inputs) {
		Update3dAudioPosition(updateParameters, inputs);

		currentTime = updateParameters.Time;
		if (synth.State == SynthesizerState.Ready) {
			synth.SpeakAsync(Text);
		}

		float visimeProgress = MathUtil.Clamp((updateParameters.Time - visemeStartTime) / visemeDuration, 0, 1);

		Channel currentChannel = visemeChannels[currentViseme];
		Channel nextChannel = visemeChannels[nextViseme];
		currentChannel?.SetValue(inputs, 1 - visimeProgress);
		nextChannel?.SetValue(inputs, visimeProgress);
	}
}
