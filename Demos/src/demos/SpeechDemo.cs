using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

public class SpeechDemo {
	public void Run() {
		var synth = new SpeechSynthesizer();

		foreach (var voice in synth.GetInstalledVoices()) {
			Console.WriteLine(voice.VoiceInfo.Name);
		}

		synth.SelectVoice("Vocalizer Lekha - Hindi For KobaSpeech 3");
		synth.VisemeReached += Synth_VisemeReached;
		synth.Speak("Hello. Ready for some fun?");
	}

	private void Synth_VisemeReached(object sender, VisemeReachedEventArgs e) {
		Console.WriteLine($"Viseme reached: {e.AudioPosition.TotalSeconds} {e.Duration.TotalSeconds} {e.Viseme} {e.NextViseme}");
	}
}
