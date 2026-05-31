using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class InteractableObjectLogic
{
	[JsonProperty]
	public string roomId;

	[JsonProperty]
	public string guid;

	public string ObjectId => roomId + "." + guid;

	public bool IsValid { get; protected set; }

	public Room host { get; private set; }

	public Vector2 position { get; private set; }

	public virtual bool shouldRender { get; protected set; }

	[JsonConstructor]
	public InteractableObjectLogic(string roomId, string guid)
	{
		this.roomId = roomId;
		this.guid = guid;
	}

	public void SetPosition(Vector2 position)
	{
		this.position = position;
	}

	public virtual void OnAfterLoadArchiveData(bool isNewGame)
	{
		IsValid &= DolocAPI.QueryRoom(roomId, out var room);
		host = room;
	}

	public void UpdatePerSecond(bool isRender)
	{
		OnUpdatePerSecond(isRender && shouldRender);
	}

	public void DailyRefresh(bool isRender)
	{
		OnDailyRefresh(isRender && shouldRender);
	}

	public void WeeklyRefresh(bool isRender)
	{
		OnWeeklyRefresh(isRender && shouldRender);
	}

	public void MonthlyRefresh(bool isRender)
	{
		OnMonthlyRefresh(isRender && shouldRender);
	}

	public void YearlyRefresh(bool isRender)
	{
		OnYearlyRefresh(isRender && shouldRender);
	}

	protected virtual void OnUpdatePerSecond(bool isRender)
	{
	}

	protected virtual void OnDailyRefresh(bool isRender)
	{
	}

	protected virtual void OnWeeklyRefresh(bool isRender)
	{
	}

	protected virtual void OnMonthlyRefresh(bool isRender)
	{
	}

	protected virtual void OnYearlyRefresh(bool isRender)
	{
	}
}
