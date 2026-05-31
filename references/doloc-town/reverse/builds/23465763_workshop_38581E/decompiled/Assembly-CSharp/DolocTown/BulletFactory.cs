using System.Collections.Generic;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class BulletFactory
{
	private readonly Transform _container;

	private readonly Dictionary<string, BulletManager> _managers = new Dictionary<string, BulletManager>();

	private readonly List<BulletManager> _managersList = new List<BulletManager>();

	private readonly Queue<(BulletManager, bool)> _changes = new Queue<(BulletManager, bool)>();

	public BulletFactory(Transform container)
	{
		_container = container;
	}

	public void ClearBullets()
	{
		foreach (BulletManager managers in _managersList)
		{
			managers.Clear();
		}
	}

	public BulletManager ChangeBulletManager(BulletManager old, BulletProto proto, BulletMoverProto moverProto)
	{
		if (old == null)
		{
			return AddBulletManager(proto, moverProto);
		}
		string text = proto.name + moverProto.name;
		if (text == old.ManagerId)
		{
			return old;
		}
		RemoveBulletManager(text);
		return AddBulletManager(proto, moverProto);
	}

	public BulletManager AddBulletManager(BulletProto proto, BulletMoverProto moverProto)
	{
		if (proto == null || moverProto == null)
		{
			return null;
		}
		string key = proto.name + moverProto.name;
		if (!_managers.TryGetValue(key, out var value))
		{
			value = new BulletManager(_container, moverProto, proto);
			_managers.Add(key, value);
			_changes.Enqueue((value, true));
		}
		value.AddReference();
		return value;
	}

	public void RemoveBulletManager(string id)
	{
		if (_managers.TryGetValue(id, out var value) && !value.RemoveReference())
		{
			value.Dispose();
			_changes.Enqueue((value, false));
			_managers.Remove(id);
		}
	}

	public void Clear()
	{
		Queue<string> queue = new Queue<string>();
		foreach (string key in _managers.Keys)
		{
			queue.Enqueue(key);
		}
		while (queue.Count > 0)
		{
			RemoveBulletManager(queue.Dequeue());
		}
	}

	public void OnFixedUpdate(float dt)
	{
		foreach (BulletManager managers in _managersList)
		{
			if (!managers.IsDisposed)
			{
				managers.OnFixedUpdate(dt);
			}
		}
		while (_changes.Count > 0)
		{
			(BulletManager, bool) tuple = _changes.Dequeue();
			if (tuple.Item2)
			{
				_managersList.Add(tuple.Item1);
			}
			else
			{
				_managersList.Remove(tuple.Item1);
			}
		}
	}

	public void OnPause()
	{
	}

	public void OnResume()
	{
	}
}
