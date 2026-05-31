using UnityEngine;

namespace DolocTown;

public class ShaderAsset : AssetBase<Shader>
{
	public ShaderAsset(string assetUrl)
		: base(assetUrl)
	{
	}

	protected override bool TryLoadAsset(string url, out Shader asset)
	{
		asset = Shader.Find(url);
		return asset != null;
	}
}
