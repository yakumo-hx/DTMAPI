using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace DTMAPI.Core.Json
{
    internal static class JsonFile
    {
        public static T Read<T>(string path)
        {
            string json = File.ReadAllText(path, new UTF8Encoding(false, true));
            if (json.Length > 0 && json[0] == '\uFEFF')
                json = json.Substring(1);

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using (var stream = new MemoryStream(bytes))
            {
                var serializer = new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true
                });
                return (T)serializer.ReadObject(stream);
            }
        }

        public static void Write<T>(string path, T value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            using (FileStream stream = File.Create(path))
            {
                var serializer = new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true
                });
                serializer.WriteObject(stream, value);
            }
        }

        public static string Prettyish(string json)
        {
            // DataContractJsonSerializer writes compact JSON; keep a tiny formatter to make configs editable.
            var builder = new StringBuilder(json.Length + 32);
            int indent = 0;
            bool inString = false;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                    inString = !inString;
                if (!inString && (c == '{' || c == '['))
                {
                    builder.Append(c).AppendLine();
                    indent++;
                    builder.Append(' ', indent * 2);
                }
                else if (!inString && (c == '}' || c == ']'))
                {
                    builder.AppendLine();
                    indent--;
                    builder.Append(' ', indent * 2).Append(c);
                }
                else if (!inString && c == ',')
                {
                    builder.Append(c).AppendLine();
                    builder.Append(' ', indent * 2);
                }
                else if (!inString && c == ':')
                {
                    builder.Append(": ");
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }
    }
}
