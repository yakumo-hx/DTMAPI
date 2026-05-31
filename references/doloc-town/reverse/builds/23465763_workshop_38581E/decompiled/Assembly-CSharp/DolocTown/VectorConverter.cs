using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class VectorConverter : JsonConverter
{
	public override bool CanRead => true;

	public override bool CanWrite => true;

	public override bool CanConvert(Type objectType)
	{
		if (!(typeof(Vector2) == objectType) && !(typeof(Vector2Int) == objectType) && !(typeof(Vector3) == objectType) && !(typeof(Vector3Int) == objectType) && !(typeof(Vector4) == objectType) && !(typeof(IEnumerable<Vector2>) == objectType) && !(typeof(IEnumerable<Vector2Int>) == objectType) && !(typeof(IEnumerable<Vector3>) == objectType) && !(typeof(IEnumerable<Vector3Int>) == objectType))
		{
			return typeof(IEnumerable<Vector4>) == objectType;
		}
		return true;
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (objectType == typeof(Vector2))
		{
			return JsonConvert.DeserializeObject<Vector2>(serializer.Deserialize(reader).ToString());
		}
		if (objectType == typeof(Vector2Int))
		{
			return JsonConvert.DeserializeObject<Vector2Int>(serializer.Deserialize(reader).ToString());
		}
		if (objectType == typeof(Vector3))
		{
			return JsonConvert.DeserializeObject<Vector3>(serializer.Deserialize(reader).ToString());
		}
		if (objectType == typeof(Vector3Int))
		{
			return JsonConvert.DeserializeObject<Vector3Int>(serializer.Deserialize(reader).ToString());
		}
		if (objectType == typeof(Vector4))
		{
			return JsonConvert.DeserializeObject<Vector4>(serializer.Deserialize(reader).ToString());
		}
		if (objectType == typeof(IEnumerable<Vector2>))
		{
			return JsonConvert.DeserializeObject<List<Vector2>>(serializer.Deserialize(reader).ToString());
		}
		if (objectType == typeof(IEnumerable<Vector2Int>))
		{
			return JsonConvert.DeserializeObject<List<Vector2Int>>(serializer.Deserialize(reader).ToString());
		}
		if (objectType == typeof(IEnumerable<Vector3>))
		{
			return JsonConvert.DeserializeObject<List<Vector3>>(serializer.Deserialize(reader).ToString());
		}
		if (objectType == typeof(IEnumerable<Vector3Int>))
		{
			return JsonConvert.DeserializeObject<List<Vector3Int>>(serializer.Deserialize(reader).ToString());
		}
		if (objectType == typeof(IEnumerable<Vector4>))
		{
			return JsonConvert.DeserializeObject<List<Vector4>>(serializer.Deserialize(reader).ToString());
		}
		throw new Exception("Unexpected Error Occurred");
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value == null)
		{
			writer.WriteNull();
		}
		else if (value is IEnumerable enumerable)
		{
			writer.WriteStartArray();
			foreach (object item in enumerable)
			{
				WriteVector(writer, item);
			}
			writer.WriteEndArray();
		}
		else
		{
			WriteVector(writer, value);
		}
	}

	private void WriteVector(JsonWriter writer, object value)
	{
		writer.WriteStartObject();
		if (!(value is Vector2 vector))
		{
			if (!(value is Vector2Int vector2Int))
			{
				if (!(value is Vector3 vector2))
				{
					if (!(value is Vector3Int vector3Int))
					{
						if (!(value is Vector4 vector3))
						{
							throw new Exception("Unexpected Error Occurred");
						}
						writer.WritePropertyName("x");
						writer.WriteValue(vector3.x);
						writer.WritePropertyName("y");
						writer.WriteValue(vector3.y);
						writer.WritePropertyName("z");
						writer.WriteValue(vector3.z);
						writer.WritePropertyName("w");
						writer.WriteValue(vector3.w);
					}
					else
					{
						writer.WritePropertyName("x");
						writer.WriteValue(vector3Int.x);
						writer.WritePropertyName("y");
						writer.WriteValue(vector3Int.y);
						writer.WritePropertyName("z");
						writer.WriteValue(vector3Int.z);
					}
				}
				else
				{
					writer.WritePropertyName("x");
					writer.WriteValue(vector2.x);
					writer.WritePropertyName("y");
					writer.WriteValue(vector2.y);
					writer.WritePropertyName("z");
					writer.WriteValue(vector2.z);
				}
			}
			else
			{
				writer.WritePropertyName("x");
				writer.WriteValue(vector2Int.x);
				writer.WritePropertyName("y");
				writer.WriteValue(vector2Int.y);
			}
		}
		else
		{
			writer.WritePropertyName("x");
			writer.WriteValue(vector.x);
			writer.WritePropertyName("y");
			writer.WriteValue(vector.y);
		}
		writer.WriteEndObject();
	}
}
