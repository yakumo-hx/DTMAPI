using RedSaw;
using UnityEngine;

namespace DolocTown;

public class FishingGameDoubleCircle : FishingGame
{
	public interface IParams
	{
		float InnerRadius { get; }

		float OuterDstToInnerRadius { get; }

		float JoyStickActiveiveRadius { get; }

		float FishAcc { get; }

		float DistanceScale { get; }

		float PullDuration { get; }

		float PullAcc { get; }

		float FishMaxSpeed { get; }
	}

	private readonly IParams gameParams;

	private readonly FishGameRendererDoubleCircle renderer;

	private readonly RSTimer fishNoiseTimer = new RSTimer();

	private Vector2 fishVelocity;

	private Vector2 fishPosition;

	private int fishVelocityNoise;

	private Vector2 fishRodPosition;

	private bool isInCircleNow;

	private float catchProgress;

	private FishingGameController.GameStatus currentGameStatus;

	public override FishingGameController.GameStatus CurrentGameStatus => currentGameStatus;

	public FishingGameDoubleCircle(DolocUserInput userInput, IParams gameParams, FishGameRendererDoubleCircle renderer)
		: base(userInput)
	{
		this.gameParams = gameParams;
		this.renderer = renderer;
	}

	private void ResetGame()
	{
		currentGameStatus = FishingGameController.GameStatus.Running;
		fishVelocity = Vector2.zero;
		fishPosition = Vector2.zero;
		fishVelocityNoise = 0;
		catchProgress = 2f;
		isInCircleNow = true;
	}

	private Vector2 GetClosestPointInCircle(Vector2 point, Vector2 center, float radius)
	{
		Vector2 vector = point - center;
		return point + vector.normalized * (radius - vector.magnitude);
	}

	private Vector2 GetVelocity(float speed, Vector2 pt, Vector2 center, float radius)
	{
		return (GetClosestPointInCircle(pt, center, radius) - pt).normalized * speed;
	}

	private Vector2 GetVerticalVelocity(Vector2 originDir, int noise)
	{
		if (noise == 0)
		{
			return Vector2.zero;
		}
		return new Vector2(0f - originDir.y, originDir.x) * noise;
	}

	public override void OnStart()
	{
		ResetGame();
		renderer.position2d = DolocAPI.mainCamera.transform.position;
		renderer.SetVisible(value: true);
	}

	public override void OnEnd()
	{
		renderer.SetVisible(value: false);
	}

	public override void OnUpdate(float dt)
	{
		if (fishNoiseTimer.Tick(Time.deltaTime))
		{
			if (Random.value < 0.5f)
			{
				fishVelocityNoise = 0;
			}
			else
			{
				fishVelocityNoise = ((!(Random.value < 0.5f)) ? 1 : (-1));
			}
			fishNoiseTimer.SetRandomInterval(0.5f, 1.5f);
		}
		UpdateRenderStatus();
	}

	public override void OnFixedUpdate(float dt)
	{
		fishPosition += fishVelocity * dt;
		fishVelocity += GetVelocity(gameParams.FishAcc, fishPosition, Vector2.zero, gameParams.OuterDstToInnerRadius);
		fishVelocity += GetVerticalVelocity(fishVelocity.normalized, fishVelocityNoise);
		fishVelocity = Vector2.ClampMagnitude(fishVelocity, gameParams.FishMaxSpeed);
		if (userInput.DeviceType == DolocInputDeviceType.KeyboardMouse)
		{
			fishRodPosition = DolocAPI.ScreenToWorld(userInput.MousePosition) - DolocAPI.cameraController.position2d;
		}
		else if (userInput.LeftJoyStickValue.magnitude == 0f)
		{
			fishRodPosition = gameParams.JoyStickActiveiveRadius * Vector2.right;
		}
		else
		{
			fishRodPosition = gameParams.JoyStickActiveiveRadius * userInput.LeftJoyStickValue;
		}
		float num = Vector2.Distance(fishRodPosition, fishPosition) / (gameParams.InnerRadius * gameParams.DistanceScale);
		fishVelocity += gameParams.PullAcc * num * (fishRodPosition - fishPosition).normalized;
		UpdateGameStatus();
	}

	private void UpdateGameStatus()
	{
		isInCircleNow = fishPosition.magnitude < gameParams.InnerRadius;
		currentGameStatus = FishingGameController.GameStatus.Running;
		if (isInCircleNow)
		{
			catchProgress += Time.deltaTime;
			if (catchProgress >= gameParams.PullDuration)
			{
				currentGameStatus = FishingGameController.GameStatus.Success;
			}
		}
		else
		{
			catchProgress -= Time.deltaTime * 1.5f;
			if (catchProgress <= 0f)
			{
				currentGameStatus = FishingGameController.GameStatus.Failed;
			}
		}
	}

	private void UpdateRenderStatus()
	{
		renderer.FishPosition = fishPosition;
		renderer.FishRodPosition = fishRodPosition;
		renderer.Progress = catchProgress / gameParams.PullDuration;
		renderer.IsCatchingNow = isInCircleNow;
	}

	public void OnDrawGizmos()
	{
		Gizmos.color = Color.white;
		Gizmos.DrawLine(Vector2.zero, fishRodPosition);
	}
}
