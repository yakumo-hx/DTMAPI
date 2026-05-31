namespace DolocTown;

public class EmailAttachNone : EmailAttachBase
{
	public override bool HasAttach => false;

	public override bool IsAccept => true;

	public override void OnFirstRead()
	{
	}

	public override bool OnAccept()
	{
		return true;
	}

	public override string ToString()
	{
		return "附件:无";
	}
}
