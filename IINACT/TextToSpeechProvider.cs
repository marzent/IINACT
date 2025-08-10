using System.Speech.Synthesis;
using System.Web;
using NAudio.Wave;

namespace IINACT;

internal class TextToSpeechProvider
{
    private readonly object speechLock = new();
    private readonly HttpClient client = new();
    private readonly SpeechSynthesizer? speechSynthesizer;
    
    public TextToSpeechProvider()
    {
        if (!Dalamud.Utility.Util.IsWine())
        {
            try
            {
                speechSynthesizer = new SpeechSynthesizer();
                speechSynthesizer?.SetOutputToDefaultAudioDevice();
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning(ex, "Failed to initialize SAPI TTS engine");
                speechSynthesizer = null;
            }
        }
        
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.TextToSpeech += Speak;
    }
    
    public void Speak(string message)
    {
        Task.Run(() =>
        {
            try
            {
                if (speechSynthesizer == null)
                    SpeakGoogle(message);
                else
                    SpeakSapi(message);
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, $"TTS failed to play back {message}");
            }
        });
    }

    private void SpeakGoogle(string message)
    {
        var query = HttpUtility.UrlEncode(message);
        const string lang = "en";
        var url = $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl={lang}&q={query}";
        var mp3Data = client.GetByteArrayAsync(url).Result;

        using var stream = new MemoryStream(mp3Data);
        using var reader = new Mp3FileReader(stream);
        using var waveOut = new WaveOutEvent();
        waveOut.Init(reader);
        var waitHandle = new ManualResetEventSlim(false);
        
        lock (speechLock)
        {
            waveOut.Play();
            waveOut.PlaybackStopped += (s, e) => waitHandle.Set();
            waitHandle.Wait();
        }
    }
    
    private void SpeakSapi(string message)
    {
        lock (speechLock)
            speechSynthesizer?.Speak(message);
    }
}
