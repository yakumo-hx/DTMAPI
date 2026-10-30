using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace DTMAPI.MultiPlatformInstaller;

internal static class JsonStore
{
    public static T? Read<T>(string path, JsonTypeInfo<T> typeInfo)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return JsonSerializer.Deserialize(stream, typeInfo);
    }

    public static string Serialize<T>(T value, JsonTypeInfo<T> typeInfo)
    {
        return JsonSerializer.Serialize(value, typeInfo);
    }

    public static void WriteAtomic<T>(string path, T value, JsonTypeInfo<T> typeInfo, Func<T?, bool>? validator = null)
    {
        string text = Serialize(value, typeInfo);
        FileSystemSafety.WriteAllTextAtomicWithRetry(path, text, validator is null
            ? null
            : candidatePath =>
            {
                T? readBack = Read(candidatePath, typeInfo);
                return validator(readBack);
            });
    }
}
