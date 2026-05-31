using DolocTown.Config.Weather;
using DolocTown.GameData;
using DolocTown.NodeCanvas;
using UnityEngine;
using UnityEngine.Serialization;

namespace DolocTown;

public abstract class SceneLight : MonoBehaviour
{
	[FormerlySerializedAs("lightScheduleGraph")]
	[SerializeField]
	private SwitchScheduleGraph switchScheduleGraph;

	[SerializeField]
	private bool disableSchedule;

	public bool IsTurnOn { get; private set; }

	public WeatherType WeatherType { get; private set; }

	public virtual void Init()
	{
	}

	private void TestSchedule()
	{
		if (!(switchScheduleGraph == null))
		{
			SwitchScheduleParams param = new SwitchScheduleParams(DolocAPI.archiveHandle.timeData.dateNow, DolocAPI.archiveHandle.CurrentWeatherType);
			Debug.Log($"当前计划表：{switchScheduleGraph.IsTrue(param)}");
		}
	}

	public void OnLightParamChanged(SwitchScheduleParams param, bool isInitial = false)
	{
		if (disableSchedule)
		{
			return;
		}
		WeatherType = param.weather;
		bool flag = ((switchScheduleGraph == null) ? DolocAPI.archiveHandle.ShouldLightUp : switchScheduleGraph.IsTrue(param));
		if (flag != IsTurnOn)
		{
			IsTurnOn = flag;
			if (flag)
			{
				TurnOn(isInitial);
			}
			else
			{
				TurnOff(isInitial);
			}
		}
	}

	public void Dispose()
	{
		IsTurnOn = false;
		base.gameObject.SetActive(value: false);
		OnDispose();
	}

	protected virtual void OnDispose()
	{
	}

	public virtual void OnUpdatePerTu()
	{
	}

	public void TurnOn()
	{
		TurnOn(isInitial: true);
	}

	protected abstract void TurnOn(bool isInitial);

	protected abstract void TurnOff(bool isInitial);
}
