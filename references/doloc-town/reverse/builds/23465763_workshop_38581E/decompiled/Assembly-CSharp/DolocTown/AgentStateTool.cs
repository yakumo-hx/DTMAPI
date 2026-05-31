using DolocTown.Config.Item;

namespace DolocTown;

public class AgentStateTool : AgentStateBase
{
	private float _endProcess;

	private string currentAnimName;

	public override bool SupportUseItem => true;

	public override bool SupportScrollQuickInventoryUI => false;

	public ItemTool tool { get; set; }

	public AgentStateTool(AgentStateManager parent, BodyController body)
		: base(parent, body)
	{
	}

	protected override AgentStateBase NextState()
	{
		if (IsAnimationDone(currentAnimName, out _endProcess))
		{
			return parent.GetState<AgentStateIdle>();
		}
		if (!status.IsGrounded)
		{
			return parent.GetState<AgentStateDrop>();
		}
		if (!status.ToolLatch && status.HorizontalMoveFactor != 0f)
		{
			return parent.GetState<AgentStateMove>();
		}
		return this;
	}

	public override void OnEnter()
	{
		_HandleTool();
		status.VelocityX = 0f;
		body.Status.ToolLatch = true;
		DolocAPI.Broadcast(OperationEventType.USE_TOOL);
	}

	private void _HandleTool()
	{
		if (tool != null)
		{
			ItemFunctionTool functionTool = tool.functionTool;
			currentAnimName = functionTool.AgentAnimName;
			body.PlayAnimation(functionTool.AgentAnimName);
			body.ToolRenderer.ResetTool(tool);
			body.ToolRenderer.ResetCostEnergyFlag();
			body.ToolRenderer.ResetChopCounter(tool.MaxChopObjects);
			body.ToolRenderer.SetVisible(value: true);
			body.ToolRenderer.Play(tool.name, functionTool.AgentAnimName);
			body.ToolRenderer.SortingLayerName = (DolocAPI.IsAgentInWater ? "GroundBack" : "GroundFront");
		}
	}

	public override void OnExit()
	{
		body.Status.ToolLatch = false;
		DolocAPI.Sound.PostSoundEvent(SoundEvents.STOP_CHARACTER_USE_TOOL);
		body.ToolRenderer.SetVisible(value: false);
	}
}
