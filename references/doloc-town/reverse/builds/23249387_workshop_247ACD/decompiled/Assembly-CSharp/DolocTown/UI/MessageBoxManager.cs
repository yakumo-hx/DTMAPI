namespace DolocTown.UI;

public class MessageBoxManager : DolocUiEntity
{
	public MessageBox messageBox { get; private set; }

	public MessageBoxLarge messageBoxLarge { get; private set; }

	public MessageBoxLittle messageBoxLittle { get; private set; }

	public MessageBoxInSceneManager messageBoxInScene { get; private set; }

	public MessageBoxInSceneWithIconManager messageBoxInSceneWithIcon { get; private set; }

	public MessageBoxNode messageBoxNode { get; private set; }

	public MessageBoxCool messageBoxCool { get; private set; }

	public MessageBoxRollCall messageBoxRollCall { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		messageBox = GetComponentInChildren<MessageBox>(includeInactive: true);
		messageBox.Init();
		messageBoxLarge = GetComponentInChildren<MessageBoxLarge>(includeInactive: true);
		messageBoxLarge.Init();
		messageBoxLittle = GetComponentInChildren<MessageBoxLittle>(includeInactive: true);
		messageBoxLittle.Init();
		messageBoxLittle.SetVisible(value: false);
		messageBoxNode = GetComponentInChildren<MessageBoxNode>(includeInactive: true);
		messageBoxNode.Init();
		messageBoxInScene = new MessageBoxInSceneManager(base.transform);
		messageBoxInSceneWithIcon = new MessageBoxInSceneWithIconManager(base.transform);
		messageBoxCool = GetComponentInChildren<MessageBoxCool>(includeInactive: true);
		messageBoxCool.Init();
		messageBoxRollCall = GetComponentInChildren<MessageBoxRollCall>(includeInactive: true);
		messageBoxRollCall.Init();
		SetVisible(value: true);
	}
}
