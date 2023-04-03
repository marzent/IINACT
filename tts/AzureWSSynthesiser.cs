using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace tts
{
    internal static class AzureWSSynthesiser
    {

        public static byte[] Synthesis(
            WebSocket ws,
            CancellationTokenSource wsCancellationSource,
            string text,
            int speed,
            int pitch,
            int volume,
            string voice,
            string style = null,
            int styleDegree = 100,
            string role = null
        )
        {
            lock (ws)
            {
                // Send request
                var requestId = Guid.NewGuid().ToString().Replace("-", "");
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
                try
                {
                    ws.SendText(
                        "Path:speech.config\r\n" +
                        $"X-RequestId:{requestId}\r\n" +
                        $"X-Timestamp:{timestamp}\r\n" +
                        "Content-Type:application/json\r\n" +
                        "\r\n" +
                        "{\"context\":{\"synthesis\":{\"audio\":{\"metadataoptions\":{\"sentenceBoundaryEnabled\":\"false\",\"wordBoundaryEnabled\":\"false\"},\"outputFormat\":\"audio-24khz-48kbitrate-mono-mp3\"}}}}\r\n",
                        wsCancellationSource
                    );

                    var ssml = CreateSSML(text, speed, pitch, volume, voice, style, styleDegree, role);
                    ws.SendText(
                        "Path:ssml\r\n" +
                        $"X-RequestId:{requestId}\r\n" +
                        $"X-Timestamp:{timestamp}\r\n" +
                        "Content-Type:application/ssml+xml\r\n" +
                        "\r\n" +
                        $"{ssml}\r\n",
                        wsCancellationSource
                    );
                }
                catch (Exception e)
                {
                    Console.WriteLine( "Synthesis"+e.ToString() );
                    ws.Abort();
                    ws.Dispose();
                    throw;
                }

                // Start receiving
                var buffer = new MemoryStream();
                var session = new WebSocketHelper.Session(ws);
                var state = ProtocolState.NotStarted;
                while (true)
                {
                    var message = WebSocketHelper.ReceiveNextMessage(session, wsCancellationSource);
                  
                    if (message.Type == WebSocketMessageType.Text)
                    {
                        if (message.MessageStr.Contains(requestId))
                        {
                            switch (state)
                            {
                                case ProtocolState.NotStarted:
                                    if (message.MessageStr.Contains("Path:turn.start"))
                                    {
                                        state = ProtocolState.TurnStarted;
                                    }

                                    break;
                                case ProtocolState.TurnStarted:
                                    if (message.MessageStr.Contains("Path:turn.end"))
                                    {
                                        throw new IOException("Unexpected turn.end");
                                    }
                                    else if (message.MessageStr.Contains("Path:turn.start"))
                                    {
                                        throw new IOException("Turn already started");
                                    }

                                    break;
                                case ProtocolState.Streaming:
                                    if (message.MessageStr.Contains("Path:turn.end"))
                                    {
                                        // All done
                                        return buffer.ToArray();
                                    }
                                    else
                                    {
                                        throw new IOException(
                                            $"Unexpected message during streaming: {message.MessageStr}");
                                    }
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        else
                        {
                            if (state != ProtocolState.NotStarted)
                            {
                                throw new IOException("Unexpected request id during streaming");
                            }
                            else
                            {
                                // Ignore
                                
                            }
                        }
                    }
                    else if (message.Type == WebSocketMessageType.Binary)
                    {
                        switch (state)
                        {
                            case ProtocolState.NotStarted:
                                // Do nothing
                                break;
                            case ProtocolState.TurnStarted:
                            case ProtocolState.Streaming:
                                // Parsing message
                                // The first 2 bytes are the header length
                                if (message.MessageBinary.Length < 2)
                                {
                                    throw new IOException("Message too short");
                                }

                                var headerLen = (message.MessageBinary[0] << 8) + message.MessageBinary[1];
                                if (message.MessageBinary.Length < 2 + headerLen)
                                {
                                    throw new IOException("Message too short");
                                }

                                var header = Encoding.UTF8.GetString(message.MessageBinary, 2, headerLen);
                                if (header.EndsWith("Path:audio\r\n"))
                                {
                                    if (!header.Contains(requestId))
                                    {
                                        throw new IOException("Unexpected request id during streaming");
                                    }

                                    state = ProtocolState.Streaming;

                                    buffer.Write(message.MessageBinary, 2 + headerLen,
                                        message.MessageBinary.Length - 2 - headerLen);
                                }
                                else
                                {
                                    Console.WriteLine($"Unexpected message with header {header}");
                                }

                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else if (message.Type == WebSocketMessageType.Close)
                    {
                        throw new IOException("Unexpected closing of connection");
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        private enum ProtocolState
        {
            NotStarted,
            TurnStarted, // turn.start received
            Streaming, // audio binary started
        }

        public static string CreateSSML(
            string text,
            int speed,
            int pitch,
            int volume,
            string voice,
            string style = null,
            int styleDegree = 100,
            string role = null
        )
        {
            if (style == "general")
            {
                style = null;
            }
            if (role == "Default")
            {
                role = null;
            }

            if (styleDegree == 0)
            {
                styleDegree = 1;
            }
            if (!string.IsNullOrWhiteSpace(style) || !string.IsNullOrWhiteSpace(role))
            {
                var sb = new StringBuilder();
                sb.Append("<mstts:express-as");
                if (!string.IsNullOrWhiteSpace(style))
                {
                    sb
                        .Append(" style=\"").Append(style).Append("\"")
                        .Append(" styledegree=\"").Append(styleDegree / 100.0f).Append("\"");
                }

                if (!string.IsNullOrWhiteSpace(role))
                {
                    sb.Append(" role=\"").Append(role).Append("\"");
                }

                sb.Append(">").Append(text).Append("</mstts:express-as>");

                text = sb.ToString();
            }

            return
                "<speak xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"http://www.w3.org/2001/mstts\" version=\"1.0\" xml:lang=\"en-US\">" +
                $"<voice name=\"{voice}\">" +
                $"<prosody rate=\"{speed - 100}%\" pitch=\"{(pitch - 100) / 2}%\" volume=\"{volume.Clamp(1, 100)}\">" +
                text +
                "</prosody></voice></speak>";
        }
    }
}
