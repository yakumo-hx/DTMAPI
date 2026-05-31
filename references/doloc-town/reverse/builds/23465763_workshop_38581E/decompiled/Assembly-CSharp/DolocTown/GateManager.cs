using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class GateManager
{
	[JsonProperty]
	private readonly HashSet<string> disabledGates;

	public string[] DisableGateList => disabledGates.ToArray();

	public GateManager(HashSet<string> disabledGates = null)
	{
		this.disabledGates = disabledGates ?? new HashSet<string>();
	}

	public bool EnableGate(string portalId)
	{
		return disabledGates.Remove(portalId);
	}

	public bool DisableGate(string portalId)
	{
		return disabledGates.Add(portalId);
	}

	public bool CheckGateEnable(string portalId)
	{
		return disabledGates.Contains(portalId);
	}
}
