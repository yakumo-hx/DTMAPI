using SimpleJSON;

namespace DolocTown.Config;

public interface ITextProvider
{
	void Load(JSONNode _json);

	void Unload();

	string GetText(string key);
}
