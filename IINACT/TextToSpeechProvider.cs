using System.Diagnostics;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;

namespace IINACT;

internal class TextToSpeechProvider
{
    private string binary = "/usr/bin/say";
    private string args = "";
    private readonly object speechLock = new();
    private readonly SpeechSynthesizer speechSynthesizer = new();  
    
    public TextToSpeechProvider()
    {
        speechSynthesizer.SetOutputToDefaultAudioDevice();
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.TextToSpeech += Speak;
    }
    
    public void Speak(string message)
    {
        if (new FileInfo(binary).Exists)
        {
            try
            {
                var ttsProcess = new Process
                {
                    StartInfo =
                    {
                        FileName = binary,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        Arguments = args + " \"" +
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
                Trace.WriteLine(ex, $"TTS failed to play back {message} with exception {ex.Message}");
                return;
            }
        }
        
        try
        {
            lock (speechLock)
                speechSynthesizer.Speak(message);
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex, $"TTS failed to play back {message} with exception {ex.Message}");
        }
        
    }
}
