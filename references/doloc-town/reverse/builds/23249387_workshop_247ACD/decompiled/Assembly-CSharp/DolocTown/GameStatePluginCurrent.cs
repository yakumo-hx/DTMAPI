using UnityEngine;

namespace DolocTown;

public class GameStatePluginCurrent : GameStatePlugin
{
	public override void BeforeSwitch(IDolocGameState lst, IDolocGameState cur)
	{
		string text = ((lst == null) ? "null" : lst.GetType().Name);
		string text2 = ((cur == null) ? "null" : cur.GetType().Name);
		Debug.Log("从" + text + "切换到" + text2);
	}
}
