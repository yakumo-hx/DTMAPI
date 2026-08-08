using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace DTMAPI.Core.Json
{
    /// <summary>
    /// Reads third-party Doloc Town official-content JSON with the narrow syntax
    /// compatibility currently used by the game's SimpleJSON loader. DTMAPI's
    /// own manifests, receipts, configuration and authority files remain on the
    /// strict <see cref="JsonFile"/> path.
    /// </summary>
    internal static class OfficialJsonCompatReader
    {
        public static T Read<T>(string path)
        {
            string json = File.ReadAllText(path, new UTF8Encoding(false, true));
            if (json.Length > 0 && json[0] == '\uFEFF')
                json = json.Substring(1);

            string compatibleJson = RemoveLineComments(json);
            byte[] bytes = Encoding.UTF8.GetBytes(compatibleJson);
            using (var stream = new MemoryStream(bytes))
            {
                var serializer = new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true
                });
                return (T)serializer.ReadObject(stream);
            }
        }

        internal static string RemoveLineComments(string json)
        {
            if (string.IsNullOrEmpty(json))
                return json ?? string.Empty;

            var builder = new StringBuilder(json.Length);
            bool inString = false;
            bool escaped = false;
            for (int index = 0; index < json.Length; index++)
            {
                char current = json[index];
                if (inString)
                {
                    builder.Append(current);
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (current == '\\')
                    {
                        escaped = true;
                    }
                    else if (current == '"')
                    {
                        inString = false;
                    }
                    continue;
                }

                if (current == '"')
                {
                    inString = true;
                    builder.Append(current);
                    continue;
                }

                if (current == '/' && index + 1 < json.Length && json[index + 1] == '/')
                {
                    index += 2;
                    while (index < json.Length && json[index] != '\r' && json[index] != '\n')
                        index++;
                    if (index < json.Length)
                        builder.Append(json[index]);
                    continue;
                }

                builder.Append(current);
            }
            return builder.ToString();
        }
    }
}
