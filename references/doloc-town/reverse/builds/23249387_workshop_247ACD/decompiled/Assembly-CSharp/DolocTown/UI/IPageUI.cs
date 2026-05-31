using System;

namespace DolocTown.UI;

public interface IPageUI<in TData> : IView
{
	Func<int, int, TData[]> DataGetter { set; }

	void SetTotalCapacity(int totalCapacity);

	void NextPage();

	void PrevPage();

	void RefreshView();

	void Select(int index);
}
