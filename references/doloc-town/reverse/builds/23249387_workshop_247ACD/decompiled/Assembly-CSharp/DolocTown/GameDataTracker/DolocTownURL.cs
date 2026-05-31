namespace DolocTown.GameDataTracker;

public static class DolocTownURL
{
	private const string BUG_REPORT_EXCEPTION_ENV = "http://146.56.231.143/doloctown/bug/exception/upload";

	private const string BUG_REPORT_EXCEPTION_LOCAL = "http://localhost:5000/doloctown/bug/exception/upload";

	public const string TRACEDATA_DAYMONEY = "http://1.13.18.185:8688/doloctown/tracedata/daymoney/upload";

	public const string TRACEDATA_DAYMISSION = "http://1.13.18.185:8688/doloctown/tracedata/daymission/upload";

	public const string TRACEDATA_DAYUPGRADE = "http://1.13.18.185:8688/doloctown/tracedata/dayupgrade/upload";

	public const string TRACEDATA_EXPRESSDRONE = "http://1.13.18.185:8688/doloctown/tracedata/expressdrone/upload";

	public static string BUG_REPORT_EXCEPTION => "http://146.56.231.143/doloctown/bug/exception/upload";
}
