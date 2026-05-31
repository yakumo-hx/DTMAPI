using System.Collections.Generic;
using System.Linq;

namespace DolocTown;

public class AutomateSystem
{
	private readonly List<IAutomateBotManager> managers = new List<IAutomateBotManager>();

	private readonly Queue<IAutomateBotManager> changes = new Queue<IAutomateBotManager>();

	private bool protection;

	public readonly AutomateSystemLocker Locker;

	private IEnumerable<AutomateBot> AllBots => managers.SelectMany((IAutomateBotManager m) => m.AllBots);

	public AutomateSystem(Room room)
	{
		Locker = new AutomateSystemLocker(room);
	}

	public void Rebuild(IEnumerable<Equipment> equipments)
	{
		changes.Clear();
		managers.Clear();
		foreach (Equipment equipment in equipments)
		{
			if (equipment is IAutomateBotManager item)
			{
				managers.Add(item);
			}
		}
	}

	public void TryAddAutomateBotManager(Equipment equipment)
	{
		if (equipment is IAutomateBotManager manager)
		{
			Add(manager);
		}
	}

	public void TryRemoveAutomateBotManager(Equipment equipment)
	{
		if (equipment is IAutomateBotManager manager)
		{
			Remove(manager);
		}
	}

	public void SetCurrentRoom(Room room)
	{
		if (room == null || room.Type != RoomType.Farm)
		{
			foreach (AutomateBot allBot in AllBots)
			{
				RecycleBot(allBot);
			}
			return;
		}
		foreach (AutomateBot allBot2 in AllBots)
		{
			if (allBot2.CurrentRoomGuid != room.Title)
			{
				RecycleBot(allBot2);
			}
			else
			{
				RenderBot(allBot2);
			}
		}
	}

	private static void RecycleBot(AutomateBot bot)
	{
		if (!(bot.Renderer == null))
		{
			AutomateBotRenderer renderer = bot.Renderer;
			bot.Renderer = null;
			DolocAPI.EntitySystem.Recycle(renderer);
		}
	}

	private static void RenderBot(AutomateBot bot)
	{
		if (!bot.IsRenderNow)
		{
			bot.Renderer = DolocAPI.EntitySystem.Next<AutomateBotRenderer>();
		}
	}

	private bool Add(IAutomateBotManager manager)
	{
		if (managers.Contains(manager))
		{
			return false;
		}
		if (protection)
		{
			changes.Enqueue(manager);
			return true;
		}
		if (DolocAPI.archiveHandle.currentRoom is TemplateRoom templateRoom && templateRoom.Title == manager.CurrentRoomGuid)
		{
			foreach (AutomateBot allBot in manager.AllBots)
			{
				RenderBot(allBot);
			}
		}
		managers.Add(manager);
		return true;
	}

	private bool Remove(IAutomateBotManager manager)
	{
		if (!managers.Contains(manager))
		{
			return false;
		}
		if (protection)
		{
			changes.Enqueue(manager);
			return true;
		}
		managers.Remove(manager);
		return true;
	}

	private void ApplyChanges()
	{
		protection = false;
		while (changes.Count > 0)
		{
			IAutomateBotManager automateBotManager = changes.Dequeue();
			if (managers.Contains(automateBotManager))
			{
				Remove(automateBotManager);
			}
			else
			{
				Add(automateBotManager);
			}
		}
	}

	public void Update()
	{
		protection = true;
		foreach (IAutomateBotManager manager in managers)
		{
			foreach (AutomateBot activatedBot in manager.ActivatedBots)
			{
				activatedBot.Update();
			}
		}
		ApplyChanges();
		Locker.Update();
	}

	public void OnBuildingChanged(Building building, bool isRemoved)
	{
		foreach (IAutomateBotManager manager in managers)
		{
			manager.OnBuildingChanged(building, isRemoved);
		}
	}
}
