using ParadoxNotion;

namespace DolocTown.NodeCanvas;

public static class NodeCanvasUtils
{
	public static string CompareLabel(this CompareMethod method)
	{
		return method switch
		{
			CompareMethod.EqualTo => "==", 
			CompareMethod.GreaterThan => ">", 
			CompareMethod.LessThan => "<", 
			CompareMethod.GreaterOrEqualTo => ">=", 
			CompareMethod.LessOrEqualTo => "<=", 
			_ => "==", 
		};
	}
}
