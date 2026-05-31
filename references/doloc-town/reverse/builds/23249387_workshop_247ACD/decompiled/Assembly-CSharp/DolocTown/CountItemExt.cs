using System.Collections.Generic;
using System.Linq;

namespace DolocTown;

public static class CountItemExt
{
	public static Item[] GenerateItems(this IEnumerable<CountItem> countItems)
	{
		return countItems.Select((CountItem x) => x.GenerateItem()).ToArray();
	}
}
