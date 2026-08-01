using System.Runtime.CompilerServices;

public static class DolocAPI
{
    public static DolocTown.BodyController agent { get; set; } = null!;

    public static bool CostEnergy(int value) => true;
    public static bool CostToolEnergy() => true;
    public static bool HasEnoughEnergy(int value) => true;
    public static bool HasEnoughEnergyForUsingTool() => true;
    public static bool CostItem(int a, int b, int c) => true;
    public static bool CostItem(int a, int b, int c, int d) => true;
    public static void CostItemNoCheck(int a, int b)
    {
    }

    public static void CostItemNoCheck(int a, int b, int c)
    {
    }

    public static bool CostSelectedItem(int a, int b) => true;
    public static bool CostSelectedItem(int a, int b, int c) => true;
    public static bool CostItemAt(int a, int b) => true;
    public static bool CanAfford(int a, int b) => true;
    public static bool CanAfford(int a, int b, int c) => true;
    public static bool CanAffordMoney(int value) => true;
}

namespace DolocTown
{
    public sealed class AgentBehaviorSettings
    {
    }

    public sealed class AgentControllerState
    {
        public void UseTool(bool pressed)
        {
        }

        public void UseItem(bool pressed)
        {
        }

        private bool EnterUICheck(
            float deltaTime,
            AgentBehaviorSettings settings) =>
            false;
    }

    public sealed class Synthesizer
    {
        public int GetRecipeTime(int recipeId, int count) =>
            60;
    }

    public sealed class BodyController
    {
        public float NativeMoveSpeed { get; set; } = 10f;
        public float MoveScaler { get; set; } = 0.35f;
        public float MoveSpeed
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => NativeMoveSpeed;
        }
    }
}
