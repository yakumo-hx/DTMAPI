using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DolocTown;

public static class JsonUtils
{
	public static bool Convert<T>(string source, out T value) where T : JToken
	{
		value = null;
		if (source != null && source.Length > 0)
		{
			try
			{
				value = JsonConvert.DeserializeObject<T>(source);
				return true;
			}
			catch (JsonException)
			{
				return false;
			}
		}
		return false;
	}

	public static bool ConvertR<T>(string source, out T value) where T : JToken
	{
		value = null;
		if (source != null && source.Length > 0)
		{
			try
			{
				value = JsonConvert.DeserializeObject<T>(source);
				return false;
			}
			catch (JsonException)
			{
				return true;
			}
		}
		return true;
	}

	public static Vector4 ToVector4(JArray arr)
	{
		return new Vector4(arr.Value<float>(0), arr.Value<float>(1), arr.Value<float>(2), arr.Value<float>(3));
	}

	public static Vector3 ToVector3(JArray arr)
	{
		return new Vector3(arr.Value<float>(0), arr.Value<float>(1), arr.Value<float>(2));
	}

	public static Vector2 ToVector2(JArray arr)
	{
		return new Vector2(arr.Value<float>(0), arr.Value<float>(1));
	}

	public static bool ToVector2(JArray arr, out Vector2 vec)
	{
		try
		{
			vec = new Vector2(arr.Value<float>(0), arr.Value<float>(1));
			return true;
		}
		catch (FormatException)
		{
			vec = Vector2.one;
			return false;
		}
	}

	public static bool GetEnum<T>(JObject obj, string key, out T value) where T : struct
	{
		value = default(T);
		if (obj.ContainsKey(key))
		{
			return Enum.TryParse<T>(obj.Value<string>(key), ignoreCase: true, out value);
		}
		return false;
	}

	public static bool GetEnumR<T>(JObject obj, string key, out T value) where T : struct
	{
		value = default(T);
		if (obj.ContainsKey(key) && Enum.TryParse<T>(obj.Value<string>(key), ignoreCase: true, out value))
		{
			return false;
		}
		return true;
	}

	public static bool GetVector2(JObject obj, string key, out Vector2 value)
	{
		value = Vector2.zero;
		if (obj.ContainsKey(key))
		{
			JArray jArray = obj.Value<JArray>(key);
			try
			{
				value = new Vector2(jArray.Value<float>(0), jArray.Value<float>(1));
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}
		return false;
	}

	public static bool GetVector2R(JObject obj, string key, out Vector2 value)
	{
		value = Vector2.zero;
		if (obj.ContainsKey(key))
		{
			JArray jArray = obj.Value<JArray>(key);
			try
			{
				value = new Vector2(jArray.Value<float>(0), jArray.Value<float>(1));
				return false;
			}
			catch (FormatException)
			{
				return true;
			}
		}
		return true;
	}

	public static bool GetVector2Int(JObject obj, string key, out Vector2Int value)
	{
		value = Vector2Int.zero;
		if (obj.ContainsKey(key))
		{
			JArray jArray = obj.Value<JArray>(key);
			try
			{
				value = new Vector2Int(jArray.Value<int>(0), jArray.Value<int>(1));
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}
		return false;
	}

	public static bool GetVector2IntR(JObject obj, string key, out Vector2Int value)
	{
		value = Vector2Int.zero;
		if (obj.ContainsKey(key))
		{
			JArray jArray = obj.Value<JArray>(key);
			try
			{
				value = new Vector2Int(jArray.Value<int>(0), jArray.Value<int>(1));
				return false;
			}
			catch (FormatException)
			{
				return true;
			}
		}
		return true;
	}

	public static bool GetByte(JObject obj, string key, out byte value)
	{
		value = 0;
		if (obj.ContainsKey(key))
		{
			try
			{
				value = obj.Value<byte>(key);
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}
		return false;
	}

	public static bool GetByteR(JObject obj, string key, out byte value)
	{
		value = 0;
		if (obj.ContainsKey(key))
		{
			try
			{
				value = obj.Value<byte>(key);
				return false;
			}
			catch (FormatException)
			{
				return true;
			}
		}
		return true;
	}

	public static bool GetInt(JObject obj, string key, out int value)
	{
		value = 0;
		if (obj.ContainsKey(key))
		{
			try
			{
				value = obj.Value<int>(key);
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}
		return false;
	}

	public static bool GetIntR(JObject obj, string key, out int value)
	{
		value = 0;
		if (obj.ContainsKey(key))
		{
			try
			{
				value = obj.Value<int>(key);
				return false;
			}
			catch (FormatException)
			{
				return true;
			}
		}
		return true;
	}

	public static bool GetFloat(JObject obj, string key, out float value)
	{
		value = 0f;
		if (obj.ContainsKey(key))
		{
			try
			{
				value = obj.Value<float>(key);
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}
		return false;
	}

	public static bool GetFloatR(JObject obj, string key, out float value)
	{
		value = 0f;
		if (obj.ContainsKey(key))
		{
			try
			{
				value = obj.Value<float>(key);
				return false;
			}
			catch (FormatException)
			{
				return true;
			}
		}
		return true;
	}

	public static bool GetString(JObject obj, string key, out string value)
	{
		value = string.Empty;
		if (obj.ContainsKey(key))
		{
			try
			{
				value = obj.Value<string>(key);
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}
		return false;
	}

	public static bool GetStringR(JObject obj, string key, out string value)
	{
		value = string.Empty;
		if (obj.ContainsKey(key))
		{
			try
			{
				value = obj.Value<string>(key);
				return false;
			}
			catch (FormatException)
			{
				return true;
			}
		}
		return true;
	}

	public static bool GetBool(JObject obj, string key, out bool value)
	{
		value = false;
		if (obj.ContainsKey(key))
		{
			try
			{
				value = obj.Value<bool>(key);
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}
		return false;
	}

	public static bool GetBoolR(JObject obj, string key, out bool value)
	{
		value = false;
		if (obj.ContainsKey(key))
		{
			try
			{
				value = obj.Value<bool>(key);
				return false;
			}
			catch (FormatException)
			{
				return true;
			}
		}
		return false;
	}

	public static bool GetArray<T>(JObject obj, string key, out T[] array)
	{
		array = null;
		if (obj.ContainsKey(key))
		{
			try
			{
				JArray jArray = obj.Value<JArray>(key);
				array = new T[jArray.Count];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = jArray.Value<T>(i);
				}
				return true;
			}
			catch (FormatException ex)
			{
				DolocAPI.outputError(ex.Message);
				return false;
			}
		}
		return false;
	}

	public static bool GetArrayR<T>(JObject obj, string key, out T[] array)
	{
		array = null;
		if (obj.ContainsKey(key))
		{
			try
			{
				JArray jArray = obj.Value<JArray>(key);
				array = new T[jArray.Count];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = jArray.Value<T>(i);
				}
				return false;
			}
			catch (FormatException ex)
			{
				DolocAPI.outputError(ex.Message);
				return true;
			}
		}
		return true;
	}

	public static bool GetObject(JObject obj, string key, out JObject output)
	{
		output = null;
		if (obj.ContainsKey(key))
		{
			output = obj.Value<JObject>(key);
			return true;
		}
		return false;
	}

	public static bool GetObjectR(JObject obj, string key, out JObject output)
	{
		output = null;
		if (obj.ContainsKey(key))
		{
			output = obj.Value<JObject>(key);
			return false;
		}
		return true;
	}

	public static Vector2Int ToVector2Int(JArray arr)
	{
		return new Vector2Int(arr.Value<int>(0), arr.Value<int>(1));
	}

	public static bool ToVector2Int(JArray arr, out Vector2Int vec)
	{
		try
		{
			vec = new Vector2Int(arr.Value<int>(0), arr.Value<int>(1));
			return true;
		}
		catch (FormatException)
		{
			vec = Vector2Int.zero;
			return false;
		}
	}

	public static bool ToVector2IntArray(JArray arr, out Vector2Int[] result)
	{
		result = new Vector2Int[arr.Count];
		for (int i = 0; i < result.Length; i++)
		{
			if (!ToVector2Int(arr.Value<JArray>(i), out result[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static Vector3Int ToVector3Int(JArray arr)
	{
		return new Vector3Int(arr.Value<int>(0), arr.Value<int>(1), arr.Value<int>(2));
	}

	public static Vector3Int[] ToVector3IntArray(JArray arr)
	{
		Vector3Int[] array = new Vector3Int[arr.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = ToVector3Int(arr.Value<JArray>(i));
		}
		return array;
	}

	public static T[] ToList<T>(JArray arr)
	{
		T[] array = new T[arr.Count];
		for (int i = 0; i < arr.Count; i++)
		{
			array[i] = arr.Value<T>(i);
		}
		return array;
	}
}
