using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class BattleSystem
{
	public readonly DroneEnv droneEnv = new DroneEnv();

	private readonly ApcManager _apcManager;

	private readonly BulletFactory _bulletFactory;

	private readonly SkillFactory _skillFactory;

	public SkillManager CreateSkillManager(string skillId)
	{
		if (!DolocAPI.assets.skills.QueryData(skillId, out var data))
		{
			return null;
		}
		return _skillFactory.AddManager(data);
	}

	public void RemoveSkillManager(SkillManager skillManager)
	{
		_skillFactory.RemoveManager(skillManager?.SkillId);
	}

	public void RemoveSkillManager(string skillId)
	{
		_skillFactory.RemoveManager(skillId);
	}

	public BulletManager CreateBulletManager(string bulletId, string bulletMoverId)
	{
		if (string.IsNullOrEmpty(bulletId))
		{
			return null;
		}
		if (!DolocAPI.assets.bullets.QueryData(bulletId, out var data))
		{
			return null;
		}
		if (!DolocAPI.assets.bulletMovers.QueryData(bulletMoverId, out var data2))
		{
			Debug.LogWarning("未知的子弹移动逻辑\"" + bulletMoverId + "\",已使用线性弹幕代替");
			data2 = new BulletMoverProtoLinear();
		}
		return _bulletFactory.AddBulletManager(data, data2);
	}

	public BulletManager ChangeBulletManager(BulletManager old, string bulletId, string bulletMoverId)
	{
		if (string.IsNullOrEmpty(bulletId))
		{
			return null;
		}
		if (!DolocAPI.assets.bullets.QueryData(bulletId, out var data))
		{
			return null;
		}
		if (!DolocAPI.assets.bulletMovers.QueryData(bulletMoverId, out var data2))
		{
			Debug.LogWarning("未知的子弹移动逻辑\"" + bulletMoverId + "\",已使用线性弹幕代替");
			data2 = new BulletMoverProtoLinear();
		}
		return _bulletFactory.ChangeBulletManager(old, data, data2);
	}

	public void RecycleBulletManager(BulletManager manager)
	{
		_bulletFactory.RemoveBulletManager(manager.ManagerId);
	}

	public void AddApc(IApc unit)
	{
		_apcManager.Add(unit);
	}

	public void RemoveBattleUnit(IApc unit)
	{
		_apcManager.Remove(unit);
	}

	public void ClearBullets()
	{
		_bulletFactory.ClearBullets();
	}

	public void ClearSkills()
	{
		_skillFactory.ClearSkills();
	}

	public void ClearBulletsAndSkills()
	{
		ClearBullets();
		ClearSkills();
	}

	public BattleSystem(Transform transform)
	{
		_apcManager = new ApcManager();
		_bulletFactory = new BulletFactory(DolocUtils.GetSubContainer(transform, "bullet"));
		_skillFactory = new SkillFactory(DolocUtils.GetSubContainer(transform, "skill"));
	}

	public void Dispose()
	{
		_skillFactory.Clear();
		_bulletFactory.Clear();
		_apcManager.Clear();
	}

	public void OnFixedUpdate(float dt)
	{
		_bulletFactory.OnFixedUpdate(dt);
		_skillFactory.OnFixedUpdate(dt);
		_apcManager.OnFixedUpdate(dt);
	}

	public void OnUpdate(float dt)
	{
		_apcManager.OnUpdate(dt);
	}

	public void OnPauseGame()
	{
		_bulletFactory.OnPause();
		_skillFactory.OnPause();
		_apcManager.OnPause();
	}

	public void OnResumeGame()
	{
		_bulletFactory.OnResume();
		_skillFactory.OnResume();
		_apcManager.OnResume();
	}
}
