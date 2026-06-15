using System.Speech.Synthesis;
using System.Web;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace IINACT;

internal class TextToSpeechProvider : IDisposable
{
    private readonly SemaphoreSlim speechLock = new(1, 1);
    private readonly HttpClient client = new();
    private readonly Configuration configuration;
    private bool disposed = false;

    public TextToSpeechProvider(Configuration config)
    {
        this.configuration = config;
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.TextToSpeech += Speak;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.TextToSpeech -= Speak;
            speechLock?.Dispose();
            client?.Dispose();
            disposed = true;
        }
    }

    public void Speak(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        Task.Run(async () =>
        {
            try
            {
                if (configuration.ConcurrentTtsPlayback)
                {
                    await ExecuteSpeak(message);
                }
                else
                {
                    await speechLock.WaitAsync();
                    try
                    {
                        await ExecuteSpeak(message);
                    }
                    finally
                    {
                        speechLock.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, $"TTS failed to play back {message}");
            }
        });
    }

    private async Task ExecuteSpeak(string message)
    {
        if (configuration.ForceGoogleTts)
            await SpeakGoogle(message);
        else
            SpeakSapi(message);
    }

    private async Task SpeakGoogle(string message)
    {
        var query = HttpUtility.UrlEncode(message);
        var lang = configuration.GoogleTtsLanguage;
        if (string.IsNullOrWhiteSpace(lang)) lang = "en";
        var url = $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl={lang}&q={query}";
        var mp3Data = await client.GetByteArrayAsync(url);

        using var stream = new MemoryStream(mp3Data);
        using var reader = new Mp3FileReader(stream);

        PlayAudioStream(reader);
    }

    private void SpeakSapi(string message)
    {
        using var speechSynthesizer = new SpeechSynthesizer();

        var voiceName = configuration.SapiVoice;
        if (!string.IsNullOrEmpty(voiceName))
        {
            speechSynthesizer.SelectVoice(voiceName);
        }

        using var stream = new MemoryStream();
        speechSynthesizer.SetOutputToWaveStream(stream);
        speechSynthesizer.Speak(message);

        stream.Position = 0;
        using var reader = new WaveFileReader(stream);

        PlayAudioStream(reader);
    }

    private void PlayAudioStream(WaveStream audioStream)
    {
        var volumeProvider = new VolumeSampleProvider(audioStream.ToSampleProvider())
        {
            Volume = float.IsNaN(configuration.TtsVolume)
                   ? 1.0f : Math.Clamp(configuration.TtsVolume, 0.0f, 2.0f)
        };

        var outputDevice = configuration.TtsPlaybackDevice;

        using var waveOut = new WaveOutEvent()
        {
            DeviceNumber = (outputDevice >= 0 && outputDevice < WaveOut.DeviceCount)
                         ? outputDevice : -1
        };
        waveOut.Init(volumeProvider);

        var waitHandle = new ManualResetEventSlim(false);
        waveOut.PlaybackStopped += (s, e) => waitHandle.Set();

        waveOut.Play();
        waitHandle.Wait();
    }
}
