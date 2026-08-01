using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace DTMAPI.Core.Runtime
{
    internal static class AuthorSessionWire
    {
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static AuthorSessionWireReadResult ReadRequest(Stream stream)
        {
            if (stream == null)
                return AuthorSessionWireReadResult.Rejected("request-stream-null", "The request stream is unavailable.");

            try
            {
                using (var buffer = new MemoryStream())
                {
                    while (true)
                    {
                        int value = stream.ReadByte();
                        if (value < 0)
                            return AuthorSessionWireReadResult.Rejected("request-frame-incomplete", "The request must end with a newline frame delimiter.");
                        if (value == '\n')
                            break;
                        if (value == '\r')
                            continue;
                        if (buffer.Length >= AuthorSessionProtocol.MaximumRequestBytes)
                        {
                            return AuthorSessionWireReadResult.Rejected(
                                "request-too-large",
                                "The request exceeds the author-session frame limit.");
                        }

                        buffer.WriteByte((byte)value);
                    }

                    if (buffer.Length == 0)
                        return AuthorSessionWireReadResult.Rejected("request-empty", "The request frame is empty.");

                    string json = StrictUtf8.GetString(buffer.ToArray());
                    using (var jsonStream = new MemoryStream(StrictUtf8.GetBytes(json), writable: false))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(AuthorSessionRequest));
                        var request = serializer.ReadObject(jsonStream) as AuthorSessionRequest;
                        return request == null
                            ? AuthorSessionWireReadResult.Rejected("request-json-invalid", "The request JSON did not contain an object.")
                            : AuthorSessionWireReadResult.FromAccepted(request);
                    }
                }
            }
            catch (DecoderFallbackException)
            {
                return AuthorSessionWireReadResult.Rejected("request-encoding-invalid", "The request must be valid UTF-8.");
            }
            catch (Exception ex)
            {
                return AuthorSessionWireReadResult.Rejected(
                    "request-json-invalid",
                    "The request is not valid protocol JSON: " + ex.GetType().Name + ".");
            }
        }

        public static void WriteResponse(Stream stream, AuthorSessionResponse response)
        {
            if (stream == null || response == null)
                return;

            byte[] payload;
            using (var buffer = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(AuthorSessionResponse));
                serializer.WriteObject(buffer, response);
                payload = buffer.ToArray();
            }

            if (payload.Length > AuthorSessionProtocol.MaximumResponseBytes)
            {
                var fallback = new AuthorSessionResponse
                {
                    Runtime = response.Runtime,
                    Session = response.Session,
                    RequestId = response.RequestId,
                    Operation = response.Operation,
                    UniqueId = response.UniqueId,
                    Status = "error",
                    Code = "response-too-large",
                    Message = "The Runtime response exceeded the author-session frame limit."
                };
                using (var buffer = new MemoryStream())
                {
                    var serializer = new DataContractJsonSerializer(typeof(AuthorSessionResponse));
                    serializer.WriteObject(buffer, fallback);
                    payload = buffer.ToArray();
                }
            }

            stream.Write(payload, 0, payload.Length);
            stream.WriteByte((byte)'\n');
            stream.Flush();
        }
    }

    internal sealed class AuthorSessionWireReadResult
    {
        private AuthorSessionWireReadResult(bool accepted, string code, string message, AuthorSessionRequest? request)
        {
            Accepted = accepted;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            Request = request;
        }

        public bool Accepted { get; }

        public string Code { get; }

        public string Message { get; }

        public AuthorSessionRequest? Request { get; }

        public static AuthorSessionWireReadResult FromAccepted(AuthorSessionRequest request) =>
            new AuthorSessionWireReadResult(true, "request-parsed", "The request frame was parsed.", request);

        public static AuthorSessionWireReadResult Rejected(string code, string message) =>
            new AuthorSessionWireReadResult(false, code, message, null);
    }
}
