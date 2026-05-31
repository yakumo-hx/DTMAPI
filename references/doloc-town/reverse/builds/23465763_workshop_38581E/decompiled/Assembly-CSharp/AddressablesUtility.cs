using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public static class AddressablesUtility
{
	public static string GetAddressFromAssetReference(AssetReference reference)
	{
		AsyncOperationHandle<IList<IResourceLocation>> handle = Addressables.LoadResourceLocationsAsync(reference);
		IList<IResourceLocation> list = handle.WaitForCompletion();
		if (list.Count > 0)
		{
			string primaryKey = list[0].PrimaryKey;
			Addressables.Release(handle);
			return primaryKey;
		}
		Addressables.Release(handle);
		return string.Empty;
	}
}
