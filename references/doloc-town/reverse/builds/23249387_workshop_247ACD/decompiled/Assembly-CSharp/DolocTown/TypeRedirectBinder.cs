using System;
using Newtonsoft.Json.Serialization;

namespace DolocTown;

public class TypeRedirectBinder : ISerializationBinder
{
	public Type BindToType(string assemblyName, string typeName)
	{
		switch (typeName)
		{
		case "DolocTown.WeatherStation":
		case "DolocTown.Computer":
			return typeof(EquipmentAnimation);
		case "DolocTown.CropCommon":
		case "DolocTown.CropGene":
			return typeof(Crop);
		default:
			return Type.GetType(typeName + ", " + assemblyName);
		}
	}

	public void BindToName(Type serializedType, out string assemblyName, out string typeName)
	{
		assemblyName = serializedType.Assembly.FullName;
		typeName = serializedType.FullName;
	}
}
