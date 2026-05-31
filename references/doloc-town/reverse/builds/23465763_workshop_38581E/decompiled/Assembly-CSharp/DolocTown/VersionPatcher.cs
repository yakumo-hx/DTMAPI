using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DolocTown;

public static class VersionPatcher
{
	public static bool TryParseVersion(this string versionStr, out Version version)
	{
		return ValidateVersionFormat(versionStr, out version);
	}

	private static bool ValidateVersionFormat(string version, out Version versionEntity)
	{
		versionEntity = null;
		if (version.IsNullOrEmpty())
		{
			return false;
		}
		string[] array = version.Split('.');
		if (array.Length != 3)
		{
			return false;
		}
		if (!int.TryParse(array[0], out var result))
		{
			return false;
		}
		if (!int.TryParse(array[1], out var result2))
		{
			return false;
		}
		if (!int.TryParse(array[2], out var result3))
		{
			return false;
		}
		versionEntity = new Version(result, result2, result3);
		return true;
	}

	private static IEnumerable<(VersionPatchAttribute, MethodInfo)> LoadAllVersionPatchFunctions()
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			Type[] types = assembly.GetTypes();
			foreach (Type type in types)
			{
				MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (MethodInfo methodInfo in methods)
				{
					if (methodInfo.GetParameters().Length == 0)
					{
						VersionPatchAttribute customAttribute = methodInfo.GetCustomAttribute<VersionPatchAttribute>();
						if (customAttribute != null)
						{
							yield return (customAttribute, methodInfo);
						}
					}
				}
			}
		}
	}

	private static Dictionary<Version, VersionPatch> LoadAllVersionPatches()
	{
		Dictionary<Version, List<(string, MethodInfo)>> dictionary = new Dictionary<Version, List<(string, MethodInfo)>>();
		foreach (var (versionPatchAttribute, item) in LoadAllVersionPatchFunctions())
		{
			if (ValidateVersionFormat(versionPatchAttribute.version, out var versionEntity))
			{
				(string, MethodInfo) item2 = (versionPatchAttribute.Description ?? string.Empty, item);
				if (dictionary.TryGetValue(versionEntity, out var value))
				{
					value.Add(item2);
					continue;
				}
				value = new List<(string, MethodInfo)> { item2 };
				dictionary.Add(versionEntity, value);
			}
		}
		return dictionary.ToDictionary((KeyValuePair<Version, List<(string, MethodInfo)>> pair) => pair.Key, delegate(KeyValuePair<Version, List<(string, MethodInfo)>> pair)
		{
			Version key = pair.Key;
			List<(string, MethodInfo)> value2 = pair.Value;
			return new VersionPatch(key, value2);
		});
	}

	public static VersionPatch[] LoadAllVersionPatchesBeyond(string archiveVersion)
	{
		if (!ValidateVersionFormat(archiveVersion, out var currentVersion))
		{
			return Array.Empty<VersionPatch>();
		}
		return (from pair in LoadAllVersionPatches()
			where pair.Key > currentVersion
			select pair.Value).ToArray();
	}
}
