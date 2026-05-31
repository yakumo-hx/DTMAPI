using System;

namespace DolocTown.UI;

public interface ISinglePageUI<in TData>
{
	Func<int, TData> DataGetter { set; }

	void SetTotalCapacity(int totalCapacity);

	void NextPage();

	void PrevPage();

	void RefreshView();

	void SelectPage(int index);
}
