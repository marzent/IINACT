using ACT.FoxTTS.engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net.WebSockets;
using NAudio.Wave;

namespace tts
{
    public class TTS : ITTSEngine
    {
        private const string URL =
           "wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1?TrustedClientToken=6A5AA1D4EAFF4E9FB37E23D68491D6F4";
        public static string CacheDirectory =>
           Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FoxTTS\\cache");

        public string Name => "EdgeTTS";

        private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();
        public static readonly Voice[] Voices = new[]
{
            new Voice("zh-CN-XiaoxiaoNeural", "中文-普通话-女 晓晓"),
            new Voice("zh-CN-XiaoyiNeural", "中文-普通话-女 晓依"),
            new Voice("zh-CN-YunjianNeural", "中文-普通话-男 云健"),
            new Voice("zh-CN-YunyangNeural", "中文-普通话-新闻-男 云扬"),
            new Voice("zh-CN-YunxiaNeural", "中文-普通话-儿童-男 云霞"),
            new Voice("zh-CN-YunxiNeural", "中文-普通话-男 云希"),

            new Voice("zh-HK-HiuMaanNeural", "中文-粤语-女 曉佳"),

            new Voice("zh-TW-HsiaoChenNeural", "中文-台普-女 曉臻"),

            new Voice("ja-JP-NanamiNeural", "日语-女 七海"),

            new Voice("en-US-AriaNeural", "英语-美国-女 阿莉雅"),
            new Voice("en-US-JennyNeural", "英语-美国-女 珍妮"),
            new Voice("en-US-GuyNeural", "英语-美国-男 盖"),

            new Voice("en-GB-SoniaNeural", "英语-英国-女 索尼娅"),
        };
        public void Speak(string text,int index)
        {
            var settings = new EdgeTTSSettings();

            settings.Voice = Voices[index].Value;
            // Xml escape
            text = SecurityElement.Escape(text);

            var wave = GetOrCreateFile(
                this,
                text,
                "mp3",
                settings.ToString(),
                f =>
                {
                    byte[] result = null;
                    var retry = 0;
                    while (true)
                    {
                        try
                        {
                            result = Synthesis(settings, text);
                            break;
                        }
                        catch (Exception e)
                        {
                            Exception se = e;
                            while (se != null)
                            {
                                if (se is SocketException)
                                {
                                    break;
                                }

                                se = se.InnerException;
                            }

                            if (se != null && ((SocketException)se).SocketErrorCode == SocketError.ConnectionReset)
                            {

                            }
                            else
                            {
                            }

                            retry++;
                            if (retry > 3)
                            {
   
                                break;
                            }
                            else
                            {
       
                            }
                        }
                    }

                    if (result != null)
                    {
                        File.WriteAllBytes(f, result);
                    }
                });

            Play(wave);
        }
        public void Play(string path)
        {
            using (var audioFile=new AudioFileReader(path))
            using (var outDevice=new WaveOutEvent())
            {
                outDevice.Init(audioFile);
                outDevice.Play();
                while (outDevice.PlaybackState==PlaybackState.Playing)
                {
                    Thread.Sleep(1000);
                }
            }

        }
        private byte[] Synthesis(EdgeTTSSettings settings, string text)
        {
            try
            {
                var ws = ObtainConnection();

                if (ws == null)
                {
                    // Cancelled
                    return null;
                }

                return AzureWSSynthesiser.Synthesis(ws, _wsCancellationSource,
                    text, settings.Speed, settings.Pitch, settings.Volume, settings.Voice);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }

        }
        private readonly CancellationTokenSource _wsCancellationSource = new CancellationTokenSource();
        private WebSocket _webSocket;
        private WebSocket ObtainConnection()
        {
            lock (this)
            {
                if (_wsCancellationSource.IsCancellationRequested)
                {
                    return null;
                }

                if (_webSocket == null)
                {
                    _webSocket = SystemClientWebSocket.CreateClientWebSocket();
                }

                switch (_webSocket.State)
                {
                    case WebSocketState.None:
                        break;
                    case WebSocketState.Connecting:
                    case WebSocketState.Open:
                        // All good
                        return _webSocket;
                    case WebSocketState.CloseSent:
                    case WebSocketState.CloseReceived:
                    case WebSocketState.Closed:
                        _webSocket.Abort();
                        _webSocket.Dispose();
                        _webSocket = SystemClientWebSocket.CreateClientWebSocket();
                        break;
                    case WebSocketState.Aborted:
                        _webSocket.Dispose();
                        _webSocket = SystemClientWebSocket.CreateClientWebSocket();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Debug.Assert(_webSocket.State == WebSocketState.None);
                // Connect

                if (_webSocket is ClientWebSocket ws)
                {
                    var options = ws.Options;
                    options.SetRequestHeader("Accept-Encoding", "gzip, deflate, br");
                    options.SetRequestHeader("Cache-Control", "no-cache");
                    options.SetRequestHeader("Pragma", "no-cache");
                }
                else
                {
                    var options = ((System.Net.WebSockets.Managed.ClientWebSocket)_webSocket).Options;
                    options.SetRequestHeader("Accept-Encoding", "gzip, deflate, br");
                    options.SetRequestHeader("Cache-Control", "no-cache");
                    options.SetRequestHeader("Pragma", "no-cache");
                }
                _webSocket.ConnectAsync(new Uri(URL), _wsCancellationSource.Token).Wait();
                return _webSocket;
            }
        }
        public string GetOrCreateFile(
           ITTSEngine engine,
           string tts,
           string ext,
           string parameter,
           Action<string> fileCreator)
        {
            // TODO: per-file lock?
            lock (this)
            {
                var newCacheFile = GetCacheFileNameNew(engine, tts, ext, parameter);

                if (File.Exists(newCacheFile))
                {

                    return newCacheFile;
                }

                var oldCacheFile = GetCacheFileNameOld(engine, tts, ext, parameter);
                if (File.Exists(oldCacheFile))
                {
                    // Rename old cache file to new file name
                    File.Move(oldCacheFile, newCacheFile);
                    return newCacheFile;
                }

                // Create the file
                if (!Directory.Exists(CacheDirectory))
                {
                    Directory.CreateDirectory(CacheDirectory);
                }
                fileCreator(newCacheFile);

                return newCacheFile;
            }
        }
        private static string GetCacheFileNameOld(
    ITTSEngine engine,
    string tts,
    string ext,
    string parameter)
        {
            tts = tts.Replace(Environment.NewLine, "+");
            var hashTTS = tts.GetHashCode().ToString("X4");
            var hashParam = parameter.GetHashCode().ToString("X4");
            var cacheName = $"{engine.Name}.{Truncate(tts, 50)}.{hashTTS}{hashParam}.{ext}";

            // ファイル名に使用できない文字を除去する
            cacheName = string.Concat(cacheName.Where(c => !InvalidChars.Contains(c)));

            var fileName = Path.Combine(
                CacheDirectory,
                cacheName);

            return fileName;
        }
        public static string Truncate(string s, int maxLength)
        {
            if (s.Length <= maxLength)
            {
                return s;
            }

            return s.Substring(0, maxLength - 1) + '\u2026';
            }
        private static string GetCacheFileNameNew(
    ITTSEngine engine,
    string tts,
    string ext,
    string parameter)
        {
            // 10 digits sha-1 hash in base36
            var hash = Hash($"{engine.Name}.{tts}.{parameter}").Substring(0, 10);

            var cacheName = $"{hash}.{ext}";

            var fileName = Path.Combine(
                CacheDirectory,
                cacheName);

            return fileName;
        }
        private static string Hash(string stringToHash)
        {
            using (var sha1 = SHA1.Create())
            {
                var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));
                return hashBytes.ToBase36String();
            }
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
