using DolocTown.GameData;
using Newtonsoft.Json;
using RedSaw.CommandLineInterface;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class MissionDecorator : Mission
{
	[DebugInfo]
	[JsonProperty]
	private string decoratorId;

	public string DecoratorId => decoratorId;

	public bool HasDecoratorId => decoratorId != null;

	public override bool IsImplicit => true;

	[JsonConstructor]
	public MissionDecorator(string chainId, string missionId, MissionContentHandle contentHandle, string decoratorId, MissionAttachModule[] modules)
		: base(chainId, missionId, contentHandle, isImplicit: true, modules)
	{
		this.decoratorId = decoratorId;
	}

	public override void CashRewards()
	{
	}
}
