using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class FactionItem
{
	[JsonProperty]
	private string _itemName;

	[JsonProperty]
	private int _count;

	[JsonProperty]
	private bool _finishState;

	public bool FinishState
	{
		get
		{
			return _finishState;
		}
		set
		{
			_finishState = value;
		}
	}

	public string ItemName => _itemName;

	public int Count => _count;

	public FactionItem(string itemName, int count)
	{
		_finishState = false;
		_itemName = itemName;
		_count = count;
	}

	[JsonConstructor]
	private FactionItem(string _itemName, int _count, bool _finishState)
	{
		this._itemName = _itemName;
		this._count = _count;
		this._finishState = _finishState;
	}
}
