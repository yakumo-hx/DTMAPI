using RedSaw;
using UnityEngine;

namespace DolocTown;

public class WindController : MonoBehaviour
{
	[SerializeField]
	private WindControllerConfig[] config;

	[SerializeField]
	private Vector2 size;

	private WindControllerConfig _config;

	private bool _hasConfigLoaded;

	private readonly RSTimer _timer = new RSTimer();

	private Vector2 RandomPosition => new Vector2(Random.Range(0f, size.x), Random.Range(0f, size.y)) + (Vector2)base.transform.position;

	public void SetEnabled(bool value)
	{
		base.gameObject.SetActive(value);
	}

	public void SetIntensity(int lv)
	{
		if (lv >= 0 && lv < config.Length)
		{
			_config = config[lv];
			_hasConfigLoaded = _config != null;
		}
	}

	private void Update()
	{
		if (_hasConfigLoaded && _timer.Tick(Time.deltaTime))
		{
			DolocAPI.effectProvider.RaiseLargeWind(RandomPosition, _config.randomSpeed, _config.randomDuration);
			_timer.SetInterval(_config.randomInterval);
		}
	}
}
