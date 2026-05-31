using UnityEngine;

namespace DolocTown;

public interface IEnvLight
{
	float intensity { get; set; }

	Color color { get; set; }

	void SetDayProcess(float process);
}
