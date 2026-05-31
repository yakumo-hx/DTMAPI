using System.Collections.Generic;
using DolocTown.GameData;
using DolocTown.Rendering;
using RedSaw;
using RedSaw.Physical;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class WaterController : MonoBehaviour
{
	[SerializeField]
	public int springCount = 51;

	[SerializeField]
	public float springConst = 0.02f;

	[SerializeField]
	public float damping = 0.04f;

	[SerializeField]
	public float spread = 0.05f;

	[SerializeField]
	protected string waterShapeName = "_WaterShape";

	[SerializeField]
	private EdgeCollider2D edgeTrigger;

	[SerializeField]
	private EdgeCollider2D edgeCollider;

	private SpriteRenderer spriteRenderer;

	[SerializeField]
	protected int waterShapeID;

	[SerializeField]
	public bool shouldWave = true;

	[SerializeField]
	[Range(0f, 1f)]
	public float simplifyThreshold = 0.5f;

	public int waterModerate = 5;

	public WaveParam waveParamDefault;

	public WaveParam waveParamOnEnter;

	public WaveParam waveParamOnStay;

	public WaveParam waveParamOnRainDrop;

	public WaveParam waveOnBomb;

	public WaveParam waveOnAttack;

	private readonly RSTimer timer = new RSTimer(3f);

	private Water2D water;

	private float[] waterShape;

	private bool isDeepWater;

	private Vector2 _waterBasePosition;

	private float _waterWidth;

	private float _waterScale;

	private static readonly int WaterBias = Shader.PropertyToID("_WaterBias");

	private Material waterMat => spriteRenderer.material;

	private void GenerateWaterShapeID()
	{
		if (string.IsNullOrEmpty(waterShapeName))
		{
			waterShapeID = -1;
		}
		else
		{
			waterShapeID = Shader.PropertyToID(waterShapeName);
		}
	}

	private void FixedUpdate()
	{
		AutoWave();
	}

	public void Init()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		water = new Water2D(springCount, waterMat.GetFloat("_WaterBias") * 4f, springConst, damping, spread);
		GenerateWaterShapeID();
		isDeepWater = !waterMat.name.Contains("shallow");
	}

	public void InitFloatingInfo()
	{
		Vector2 vector = base.transform.position;
		Vector2 vector2 = (Vector2)spriteRenderer.sprite.bounds.size * (Vector2)base.transform.localScale;
		float @float = waterMat.GetFloat("_WaterBias");
		_waterBasePosition = new Vector2(vector.x, vector.y + vector2.y * @float);
		_waterWidth = vector2.x;
		_waterScale = waterMat.GetFloat("_WaterScale") * vector2.y;
		Debug.Log($"水体浮动信息:水面起点{_waterBasePosition},宽度{_waterWidth},缩放{_waterScale}");
	}

	public float CalcDistanceError(Vector2 pos)
	{
		return water.CalcDistanceErr(pos, _waterBasePosition, _waterWidth, _waterScale);
	}

	private void AutoWave()
	{
		if (!(waterMat == null) || waterShapeID <= 0)
		{
			if (shouldWave && timer.Tick(Time.fixedDeltaTime))
			{
				int index = ((Random.value < 0.5f) ? 1 : (springCount - 2));
				water.Wave(index, Random.value * waveParamDefault.intensity, waveParamDefault.spread);
			}
			waterShape = (isDeepWater ? water.UpdateSeamless(Time.fixedDeltaTime) : water.Update(Time.fixedDeltaTime));
			waterMat.SetFloatArray(waterShapeID, waterShape);
			ResetColliderShape();
		}
	}

	public void Wave(Vector2 pos, WaveParam waveParam, bool reverse = false)
	{
		int num = ((!reverse) ? 1 : (-1));
		Wave(pos, waveParam.intensity * (float)num, waveParam.spread);
	}

	public void Wave(Vector2 pos, float intensity, float spread)
	{
		if (!(spriteRenderer == null))
		{
			Vector2 vector = spriteRenderer.bounds.size;
			Vector2 vector2 = (Vector2)base.transform.position - vector * spriteRenderer.sprite.pivot;
			int index = Mathf.RoundToInt((pos.x - vector2.x) / vector.x * (float)springCount);
			water.Wave(index, intensity, spread);
		}
	}

	private void ResetColliderShape()
	{
		float num = 4f / (float)(waterShape.Length - 1);
		float num2 = waterMat.GetFloat(WaterBias) * 4f;
		List<Vector2> list = new List<Vector2>();
		for (int i = 0; i < waterShape.Length; i++)
		{
			list.Add(new Vector2(num * (float)i, num2 + waterShape[i]));
		}
		Vector2[] points = PhysicalUtils.SimplifyPolyLine(list.ToArray(), simplifyThreshold * 0.01f);
		edgeTrigger.points = points;
		edgeCollider.points = points;
	}

	private void DebugInit()
	{
		Init();
		GetComponent<RenderTextureUVHandler>().InitRenderInfo(DolocAPI.worldResolution);
	}

	public void ResetParams()
	{
		water.SetParams(springConst, damping, spread);
	}

	public void Clear()
	{
		water.Clear();
	}

	public void WaveTest(int index, float intensity, float spread)
	{
		water.Wave(index, intensity, spread);
	}
}
