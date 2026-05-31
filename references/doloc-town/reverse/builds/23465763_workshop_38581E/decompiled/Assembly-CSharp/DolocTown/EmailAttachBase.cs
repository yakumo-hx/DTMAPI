using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public abstract class EmailAttachBase
{
	public abstract bool IsAccept { get; }

	public abstract bool HasAttach { get; }

	public abstract void OnFirstRead();

	public abstract bool OnAccept();
}
