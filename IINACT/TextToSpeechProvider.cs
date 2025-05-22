using System.Diagnostics;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;

namespace IINACT;

internal class TextToSpeechProvider
{
    private string binary = "/usr/bin/say";
    private string args = "";
    private readonly object speechLock = new();

    public TextToSpeechProvider()
    {
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.TextToSpeech += Speak;
    }

    public void Speak(string message)
    {
        Task.Run(() =>
        {
            if (new FileInfo(binary).Exists)
            {
                try
                {
                    var ttsProcess = new Process
                    {
                        StartInfo =
                        {
                            FileName = "C:\\windows\\system32\\start.exe",
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            Arguments = $"/unix {binary} {args} \"" +
                                        Regex.Replace(Regex.Replace(message, @"(\\*)" + "\"", @"$1$1\" + "\""),
                                                      @"(\\+)$", @"$1$1") + "\""
                        }
                    };
                    lock (speechLock)
                    {
                        ttsProcess.Start();
                        // heuristic pause
                        Thread.Sleep(500 * message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
                    }

                    return;
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error(ex, $"TTS failed to play back {message}");
                    return;
                }
            }
            Plugin.Log.Error("tts");
            try
            {
                SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();
                speechSynthesizer.SetOutputToDefaultAudioDevice();
                speechSynthesizer.SpeakAsync(message);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, $"TTS failed to play back {message}");
            }
        });
    }
}
