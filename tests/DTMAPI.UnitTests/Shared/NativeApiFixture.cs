using System;
using System.Reflection;

public static partial class DolocAPI
{
    public sealed class FakeUserInput
    {
        public object? CurrentState { get; set; }
    }

    public static object? gameManager;

    public static object? dataPersistenceManager;

    public static object? agent;

    public static object? timeScaleManager;

    public static object? userInput;

    public static object? GlobalParameter;

    public static object? archiveHandle;

    public static int CommandSetWeatherCalls;

    public static string LastSetWeatherId = string.Empty;

    public static bool LastSetWeatherPatch;

    public static bool SuppressWeatherCommandMutation;

    public static int CostEnergyCalls;

    public static int LastEnergyCost;

    public static bool CostEnergy(int value)
    {
        CostEnergyCalls++;
        LastEnergyCost = value;
        return true;
    }

    public static void SetTimeScale(
        float multiplier,
        bool showTip)
    {
        object manager = timeScaleManager ??
            throw new InvalidOperationException(
                "Fake timeScaleManager is unavailable.");
        PropertyInfo property =
            manager.GetType().GetProperty(
                "currentTimeScale",
                BindingFlags.Public |
                BindingFlags.Instance) ??
            throw new MissingMemberException(
                manager.GetType().FullName,
                "currentTimeScale");
        property.SetValue(manager, multiplier, null);
    }

    public static void ResetWeatherCommandFixture()
    {
        CommandSetWeatherCalls = 0;
        LastSetWeatherId = string.Empty;
        LastSetWeatherPatch = false;
        SuppressWeatherCommandMutation = false;
    }

    private static void Command_SetWeather(
        string weather,
        bool patch)
    {
        CommandSetWeatherCalls++;
        LastSetWeatherId = weather;
        LastSetWeatherPatch = patch;
        if (SuppressWeatherCommandMutation || archiveHandle == null)
            return;
        PropertyInfo property = archiveHandle.GetType().GetProperty(
            "LocalWeatherType",
            BindingFlags.Public | BindingFlags.Instance) ??
            throw new MissingMemberException(
                archiveHandle.GetType().FullName,
                "LocalWeatherType");
        object value = property.PropertyType == typeof(string)
            ? weather
            : Enum.Parse(property.PropertyType, weather, true);
        property.SetValue(archiveHandle, value, null);
    }
}
