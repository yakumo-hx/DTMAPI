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
            return Deserialize<T>(json);
        }

        internal static T Deserialize<T>(string json)
        {
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

    }
}
