namespace DolocTown;

public interface IEquipmentWorkerRenderer
{
	[WorkerRenderer("default")]
	public class WorkerRendererDefault : IEquipmentWorkerRenderer
	{
		void IEquipmentWorkerRenderer.OnWork()
		{
		}

		void IEquipmentWorkerRenderer.OnFailed()
		{
		}

		void IEquipmentWorkerRenderer.OnIdle()
		{
		}

		void IEquipmentWorkerRenderer.OnStop()
		{
		}

		void IEquipmentWorkerRenderer.OnRemove()
		{
		}

		void IEquipmentWorkerRenderer.OnWorkDone()
		{
		}
	}

	static readonly IEquipmentWorkerRenderer Default;

	void OnWork();

	void OnIdle();

	void OnFailed();

	void OnStop();

	void OnRemove();

	void OnWorkDone();

	static IEquipmentWorkerRenderer()
	{
		Default = new WorkerRendererDefault();
	}
}
