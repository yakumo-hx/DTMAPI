using UnityEngine;

namespace RedSaw;

public interface IBlackboard
{
	T GetComponent<T>() where T : MonoBehaviour;

	T Read<T>();

	T Read<T>(string alias);

	void Write<T>(T value);

	void Write<T>(T value, string alias);

	T ReadGlobal<T>();

	T ReadGlobal<T>(string alias);

	void WriteGlobal<T>(T value);

	void WriteGlobal<T>(T value, string alias);
}
