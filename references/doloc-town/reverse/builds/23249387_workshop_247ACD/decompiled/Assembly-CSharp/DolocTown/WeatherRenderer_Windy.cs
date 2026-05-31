using DolocTown.Config.Weather;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(ParticleSystem))]
public class WeatherRenderer_Windy : WeatherRenderer
{
	[SerializeField]
	private ParticleSystem ps;

	[SerializeField]
	private WeatherRenderInfoSO renderInfo;

	public override WeatherType WeatherType => WeatherType.WINDY;

	public override WeatherRenderInfoSO RenderInfo => renderInfo;

	public override void SetEnable(bool enable, float dayProcess, bool transit)
	{
		SetVisible(enable);
		if (enable)
		{
			ps.Play();
			ParticleSystem[] componentsInChildren;
			if (RandomUtils.Dice(0.33f))
			{
				componentsInChildren = ps.GetComponentsInChildren<ParticleSystem>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].Play();
				}
				return;
			}
			componentsInChildren = ps.GetComponentsInChildren<ParticleSystem>();
			foreach (ParticleSystem particleSystem in componentsInChildren)
			{
				if (!(particleSystem == ps))
				{
					particleSystem.Stop();
				}
			}
		}
		else
		{
			ps.Stop(withChildren: true);
		}
	}
}
