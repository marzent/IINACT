using System.Diagnostics;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using Dalamud.Logging;

namespace IINACT;

internal class TextToSpeechProvider
{
    private string binary = "/usr/bin/say";
    private string args = "";
    private readonly object speechLock = new();
    private readonly SpeechSynthesizer? speechSynthesizer;  
    
    public TextToSpeechProvider()
    {
        try
        {
            speechSynthesizer = new SpeechSynthesizer();
            speechSynthesizer?.SetOutputToDefaultAudioDevice();
        }
        catch (Exception ex)
        {
            PluginLog.Warning(ex, "Failed to initialize SAPI TTS engine");
        }
        
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
                PluginLog.Error(ex, $"TTS failed to play back {message}");
                return;
            }
        }
        
        try
        {
            lock (speechLock)
                speechSynthesizer?.Speak(message);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"TTS failed to play back {message}");
        }
        
    }
}
