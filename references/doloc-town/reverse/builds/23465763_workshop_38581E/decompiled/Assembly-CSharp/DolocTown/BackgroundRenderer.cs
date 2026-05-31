using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class BackgroundRenderer : DolocObject
{
	[SerializeField]
	private Camera cam;

	[SerializeField]
	private Shader envShader;

	[SerializeField]
	private GameObject layerPrefab;

	[SerializeField]
	private EnvBackgroundSO defaultPreset;

	private ObjectPool<BackgroundLayerRenderer> pool;

	private float yOrigin;

	private BackgroundLayerRenderer skyRenderer;

	private BackgroundLayerRenderer[] _layers;

	private EnvBackgroundSO _currentPreset;

	protected override void __Init()
	{
		base.__Init();
		skyRenderer = Object.Instantiate(layerPrefab, base.transform).GetComponent<BackgroundLayerRenderer>();
		pool = new ObjectPool<BackgroundLayerRenderer>(layerPrefab, base.transform);
		yOrigin = cam.transform.position.y;
		LoadPreset(defaultPreset);
		SetVisible(value: false);
	}

	public void LoadDefaultPreset()
	{
		if (defaultPreset == null)
		{
			Debug.LogError("默认背景预设为空");
		}
		else
		{
			LoadPreset(defaultPreset);
		}
	}

	public void SetBackgroundOffset(float XOffset)
	{
		float num = yOrigin - cam.transform.position.y;
		for (int i = 0; i < _currentPreset.Layers.Length; i++)
		{
			BackgroundLayer backgroundLayer = _currentPreset.Layers[i];
			_layers[i].UpdatePosition(backgroundLayer.moveSpeedX, XOffset, backgroundLayer.fixedOffsetX, backgroundLayer.moveSpeedY * num);
		}
	}

	public void SetBackgroundMask(bool value)
	{
		skyRenderer.SetMaskable(value);
		BackgroundLayerRenderer[] layers = _layers;
		for (int i = 0; i < layers.Length; i++)
		{
			layers[i].SetMaskable(value);
		}
	}

	public void LoadPreset(EnvBackgroundSO bg)
	{
		if (!(bg == null))
		{
			_currentPreset = bg;
			skyRenderer.LoadLayer(bg.Sky.sprite, envShader, bg.Sky.saturation, bg.Sky.brightness, bg.Sky.sortingLayer, bg.Sky.color, bg.Sky.orderInLayer, bg.Sky.hasAlpha, bg.Sky.alpha);
			_layers = new BackgroundLayerRenderer[bg.Layers.Length];
			pool.CheckCount(bg.Layers.Length);
			for (int i = 0; i < bg.Layers.Length; i++)
			{
				BackgroundLayerRenderer backgroundLayerRenderer = pool.Instances[i];
				backgroundLayerRenderer.LoadLayer(bg.Layers[i].sprite, envShader, bg.Layers[i].saturation, bg.Layers[i].brightness, bg.Layers[i].sortingLayer, bg.Layers[i].color, bg.Layers[i].orderInLayer, bg.Layers[i].hasAlpha, bg.Layers[i].alpha);
				_layers[i] = backgroundLayerRenderer;
			}
		}
	}

	public void TryInit()
	{
		Init();
	}

	public void SetYOrigin(float y)
	{
		yOrigin = y;
	}

	private void Update()
	{
		if (!base.IsNotInitialized && !(_currentPreset == null))
		{
			float num = yOrigin - cam.transform.position.y;
			for (int i = 0; i < _currentPreset.Layers.Length; i++)
			{
				BackgroundLayer backgroundLayer = _currentPreset.Layers[i];
				_layers[i].UpdatePosition(backgroundLayer.moveSpeedX, backgroundLayer.offsetX * Time.deltaTime, backgroundLayer.fixedOffsetX, backgroundLayer.moveSpeedY * num);
			}
		}
	}
}
