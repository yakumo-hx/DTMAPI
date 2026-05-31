using RedSaw;

namespace DolocTown;

public interface IEquipmentWorker
{
	int WorkScale { get; }

	IEquipmentWorkerRenderer WorkRenderer { get; }

	IEquipmentWorkerRenderer ExtraWorkerRenderer { get; }

	IEquipmentWorkerRenderer LowPowerWorkerRenderer { get; }

	Counter WorkCounter { get; }

	bool IsWorking { get; set; }

	bool IsIdle { get; set; }

	void UpdateWorker()
	{
		if (!IsWorking)
		{
			return;
		}
		if (IsIdle)
		{
			if (Launch())
			{
				IsIdle = false;
				WorkRenderer.OnWork();
				ExtraWorkerRenderer?.OnWork();
				LowPowerWorkerRenderer?.OnWork();
			}
			else
			{
				WorkRenderer.OnFailed();
				ExtraWorkerRenderer?.OnFailed();
				LowPowerWorkerRenderer?.OnFailed();
			}
			return;
		}
		OnWorking(isRender: true);
		if (IsWorking)
		{
			if (WorkCounter.Tick())
			{
				IsWorking = false;
				WorkRenderer.OnStop();
				ExtraWorkerRenderer?.OnStop();
				LowPowerWorkerRenderer?.OnStop();
				FinishWork();
			}
			else if (!Launch())
			{
				IsIdle = true;
				WorkRenderer.OnIdle();
				ExtraWorkerRenderer?.OnIdle();
				LowPowerWorkerRenderer?.OnIdle();
			}
		}
	}

	void UpdateWorkerNoRender()
	{
		if (!IsWorking)
		{
			return;
		}
		if (IsIdle)
		{
			if (Launch())
			{
				IsIdle = false;
			}
			return;
		}
		OnWorking(isRender: false);
		if (IsWorking)
		{
			if (WorkCounter.Tick())
			{
				IsWorking = false;
				FinishWorkNoRender();
			}
			else if (!Launch())
			{
				IsIdle = true;
			}
		}
	}

	bool Work(int n)
	{
		if (IsWorking || n <= 0)
		{
			return false;
		}
		ForceWork(n);
		return true;
	}

	bool WorkNoRender(int n)
	{
		if (IsWorking || n <= 0)
		{
			return false;
		}
		ForceWorkNoRender(n);
		return true;
	}

	void ForceWork(int n)
	{
		IsWorking = true;
		WorkCounter.SetInterval(n);
		if (Launch())
		{
			IsIdle = false;
			WorkRenderer.OnWork();
			ExtraWorkerRenderer?.OnWork();
			LowPowerWorkerRenderer?.OnWork();
		}
		else
		{
			IsIdle = true;
			WorkRenderer.OnIdle();
			ExtraWorkerRenderer?.OnIdle();
			LowPowerWorkerRenderer?.OnIdle();
		}
	}

	void ForceWorkNoRender(int n)
	{
		IsWorking = true;
		WorkCounter.SetInterval(n);
		IsIdle = !Launch();
	}

	void ForceStopWork()
	{
		IsWorking = false;
		WorkCounter.SetInterval(0);
		IsIdle = false;
		WorkRenderer?.OnStop();
		ExtraWorkerRenderer?.OnStop();
		LowPowerWorkerRenderer?.OnStop();
	}

	void OnWorking(bool isRender);

	bool Launch();

	void FinishWork();

	void FinishWorkNoRender();
}
