using DolocTown.Config.Weather;
using DolocTown.GameData;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("天气检查", 0)]
[Description("检查当前天气的状态")]
public class DialogueTask_WeatherCheck : DialogueConditionTask
{
	private enum CheckType
	{
		WeatherType,
		IsMalignantWeather,
		IsRainy
	}

	[SerializeField]
	private CheckType _checkType;

	[SerializeField]
	private WeatherType _weatherType;

	[SerializeField]
	private bool _isMalignantWeather;

	[SerializeField]
	private bool _isRainy;

	public override string taskTitle => _checkType switch
	{
		CheckType.IsRainy => (_isRainy ? "是" : "不是") + "雨天", 
		CheckType.WeatherType => "当前天气类型为" + _weatherType, 
		CheckType.IsMalignantWeather => (_isMalignantWeather ? "是" : "不是") + "恶性天气", 
		_ => "??", 
	};

	protected override bool CheckCondition()
	{
		WeatherType currentWeatherType = DolocAPI.archiveHandle.CurrentWeatherType;
		return _checkType switch
		{
			CheckType.IsRainy => currentWeatherType.IsRainyWeather() == _isRainy, 
			CheckType.IsMalignantWeather => currentWeatherType.IsMalignantWeather() == _isMalignantWeather, 
			CheckType.WeatherType => currentWeatherType == _weatherType, 
			_ => false, 
		};
	}
}
