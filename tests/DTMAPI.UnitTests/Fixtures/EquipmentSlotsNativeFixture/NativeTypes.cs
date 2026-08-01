public static class DolocAPI
{
    public static object? archiveHandle =
        new FixtureArchiveHandle();
    public static object? dataPersistenceManager;
    public static bool BackpackPlacementAvailable = true;
    public static System.Func<string, int, bool>?
        SendItemAsEmailOverride;
    public static int SendItemAsEmailCallCount;
    public static DolocTown.HurtReason LastHurtReason;
    public static int LastHealthCost;
    public static bool CostHealthReturnsDead;
    public static int DamageTipCount;
    public static int HurtBroadcastCount;

    private static readonly System.Collections.Generic.Dictionary<string, int>
        Backpack =
            new System.Collections.Generic.Dictionary<string, int>(
                System.StringComparer.Ordinal);

    public static int CountItem(string itemId, bool includeEquipment) =>
        Backpack.TryGetValue(
            itemId ?? string.Empty,
            out int count)
            ? count
            : 0;

    public static bool CanPlaceItem(string itemId, int count) =>
        BackpackPlacementAvailable &&
        !string.IsNullOrWhiteSpace(itemId) &&
        count > 0;

    public static bool TryPlaceInBackpack(
        string itemId,
        int count,
        bool sendByMail)
    {
        if (!CanPlaceItem(itemId, count))
            return false;
        Backpack[itemId] = CountItem(itemId, false) + count;
        return true;
    }

    public static bool SendItemAsEmail(
        string itemId,
        int count,
        string? title,
        string? content,
        string? sender,
        string templateId)
    {
        SendItemAsEmailCallCount++;
        return SendItemAsEmailOverride?.Invoke(
            itemId,
            count) ?? false;
    }

    public static bool CostItem(
        string itemId,
        int count,
        bool includeEquipment)
    {
        int current = CountItem(itemId, includeEquipment);
        if (string.IsNullOrWhiteSpace(itemId) ||
            count <= 0 ||
            current < count)
        {
            return false;
        }
        Backpack[itemId] = current - count;
        return true;
    }

    public static void ResetInventory()
    {
        Backpack.Clear();
        archiveHandle = new FixtureArchiveHandle();
        dataPersistenceManager = null;
        BackpackPlacementAvailable = true;
        SendItemAsEmailOverride = null;
        SendItemAsEmailCallCount = 0;
    }

    public static void SetItemCount(string itemId, int count)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            throw new System.ArgumentException(
                "A fixture item identity is required.",
                nameof(itemId));
        Backpack[itemId] = System.Math.Max(0, count);
    }

    public static bool CostHealth(
        int damage,
        DolocTown.HurtReason reason)
    {
        LastHealthCost = damage;
        LastHurtReason = reason;
        return CostHealthReturnsDead;
    }

    public static void RaiseDamageTip(
        int damage,
        UnityEngine.Vector2 position,
        bool critical,
        float duration,
        float height,
        float scale) =>
        DamageTipCount++;

    public static void Broadcast(
        DolocTown.GameEventType eventType)
    {
        if (eventType ==
            DolocTown.GameEventType.HURT_BY_MONSTER)
        {
            HurtBroadcastCount++;
        }
    }

    public static void ResetAttackEvidence()
    {
        LastHurtReason = DolocTown.HurtReason.None;
        LastHealthCost = 0;
        CostHealthReturnsDead = false;
        DamageTipCount = 0;
        HurtBroadcastCount = 0;
        DolocTown.AgentStateFishing.UnsetUiControlCount = 0;
    }
}

public sealed class FixtureArchiveHandle
{
    public FixtureFarmData farmData { get; set; } =
        new FixtureFarmData();
}

public sealed class FixtureFarmData
{
    public FixtureEmailManager emailManager { get; set; } =
        new FixtureEmailManager();
}

public sealed class FixtureEmailManager
{
    public object emails { get; set; } =
        System.Array.Empty<object>();
}

public sealed class FixtureEmail
{
    public string Id { get; set; } =
        "send_item_template";

    public object emailAttaches { get; set; } =
        System.Array.Empty<object>();
}

namespace UnityEngine
{
    public struct Vector2
    {
    }
}

namespace DolocTown.GameData
{
    public sealed class EquipmentAbilityData
    {
        public EquipmentAbilityData(
            int defence,
            float moveSpeedAddition,
            bool immuneAcidRain,
            float dashCdDecrease,
            float recoveryAdditionPercent,
            int fellCoundAdditionOre,
            string[] shieldSightOfMonsterNames,
            float criticalRateChanged)
        {
            this.defence = defence;
            this.moveSpeedAddition = moveSpeedAddition;
            this.immuneAcidRain = immuneAcidRain;
            this.dashCdDecrease = dashCdDecrease;
            this.recoveryAdditionPercent = recoveryAdditionPercent;
            this.fellCoundAdditionOre = fellCoundAdditionOre;
            ShieldSightOfMonsterNames = shieldSightOfMonsterNames;
            CriticalRateChanged = criticalRateChanged;
        }

        public int defence;
        public float moveSpeedAddition;
        public bool immuneAcidRain;
        public float dashCdDecrease;
        public float recoveryAdditionPercent;
        public int fellCoundAdditionOre;
        public string[] ShieldSightOfMonsterNames;
        public float CriticalRateChanged;
    }

    public sealed class AgentEquipmentManager
    {
        public readonly System.Collections.IDictionary functions =
            new System.Collections.Hashtable();

        public int BaseDefense { get; set; }

        public EquipmentAbilityData EquipmentAbility { get; set; } =
            new EquipmentAbilityData(
                0,
                0f,
                false,
                0f,
                0f,
                0,
                System.Array.Empty<string>(),
                0f);

        public int ReloadCount { get; private set; }

        public bool ThrowOnReload { get; set; }

        public bool HasNativeShield { get; set; }

        public bool ThrowOnShieldProbe { get; set; }

        public void ReloadParams()
        {
            ReloadCount++;
            if (ThrowOnReload)
            {
                throw new System.InvalidOperationException(
                    "fixture ReloadParams failure");
            }
            EquipmentAbility =
                new EquipmentAbilityData(
                    BaseDefense,
                    0f,
                    false,
                    0f,
                    0f,
                    0,
                    System.Array.Empty<string>(),
                    0f);
        }

        public bool TryGetShieldItem(out object? item)
        {
            if (ThrowOnShieldProbe)
            {
                throw new System.InvalidOperationException(
                    "fixture native shield probe failure");
            }
            item = HasNativeShield ? new object() : null;
            return item != null;
        }
    }
}

namespace DolocTown
{
    public sealed class EmailAttachReward
    {
        public bool isAccept { get; set; }

        public RewardItem reward { get; set; } =
            new RewardItem();
    }

    public sealed class RewardItem
    {
        public string itemName { get; set; } =
            string.Empty;

        public int itemCount { get; set; }
    }

    public sealed class QuestEmailAttachReward
    {
        public bool isAccept { get; set; }

        public RewardItem reward { get; set; } =
            new RewardItem();
    }

    public class Item
    {
        public FixtureItemProto proto { get; set; } =
            new FixtureItemProto();
    }

    public class ItemHat : Item
    {
    }

    public sealed class FixtureItemHatShield : ItemHat
    {
    }

    public sealed class FixtureItemProto
    {
        public string Title { get; set; } =
            "fixture shield";

        public object Function { get; set; } =
            new FixtureItemFunctionHatShield();
    }

    public sealed class FixtureItemFunctionHatShield
    {
        public FixtureHatInfo HatId_Ref { get; set; } =
            new FixtureHatInfo();

        public int MaxShieldValue { get; set; } = 10;
    }

    public sealed class FixtureHatInfo
    {
        public string Skill { get; set; } = "shield";

        public int Defense { get; set; } = 7;

        public FixtureSkillInfo Skill_Ref { get; set; } =
            new FixtureSkillInfo();
    }

    public sealed class FixtureSkillInfo
    {
        public FixtureAgentEquipmentFuncProtoShield Function
        {
            get;
            set;
        } = new FixtureAgentEquipmentFuncProtoShield();
    }

    public sealed class FixtureAgentEquipmentFuncProtoShield
    {
        public int Defend { get; set; } = 2;
    }

    public static class ItemFactory
    {
        public static bool ThrowOnGenerate { get; set; }

        public static int ShieldMaxValue { get; set; } = 10;

        public static bool GenerateItem(
            string itemId,
            int count,
            out Item? item)
        {
            if (ThrowOnGenerate)
            {
                throw new System.InvalidOperationException(
                    "fixture native item lookup failure");
            }
            item = string.Equals(
                    itemId,
                    "fixture-shield",
                    System.StringComparison.Ordinal)
                ? new FixtureItemHatShield()
                : null;
            if (item?.proto.Function is
                FixtureItemFunctionHatShield shield)
            {
                shield.MaxShieldValue = ShieldMaxValue;
            }
            return item != null;
        }
    }

    public enum HurtReason
    {
        None = 0,
        MonsterAttack = 1
    }

    public enum GameEventType
    {
        HURT_BY_MONSTER = 0
    }

    public static class BattleUtils
    {
        public static int CalcDamage(
            float attack,
            float defend,
            bool critical) =>
            System.Math.Max(
                0,
                (int)System.Math.Round(attack - defend));
    }

    public class AgentStateFishing
    {
        public static int UnsetUiControlCount { get; set; }

        public static void UnsetUiControl() =>
            UnsetUiControlCount++;
    }

    public sealed class AgentStateHit
    {
    }

    public sealed class FixtureStateManager
    {
        public object? current;

        public System.Type? LastOverwriteType { get; private set; }

        public bool LastShouldQuit { get; private set; }

        public void Overwrite<T>(bool shouldQuit = true)
        {
            LastOverwriteType = typeof(T);
            LastShouldQuit = shouldQuit;
        }
    }

    public sealed class FixtureHatRenderer
    {
        public int ShineCount { get; private set; }

        public void Shine() =>
            ShineCount++;
    }

    public sealed class FixtureFishRodRenderer
    {
        public bool Visible { get; private set; } = true;

        public void SetVisible(bool value) =>
            Visible = value;
    }

    public sealed class BodyController
    {
        public bool IsFaint { get; set; }

        public float CurrentDefend { get; set; }

        public FixtureStateManager StateManager { get; } =
            new FixtureStateManager();

        public FixtureHatRenderer HatRenderer { get; } =
            new FixtureHatRenderer();

        public FixtureFishRodRenderer fishRodRenderer { get; } =
            new FixtureFishRodRenderer();

        public int HitBackCount { get; private set; }

        public bool OnAttacked(
            float attack,
            bool criticalRate,
            UnityEngine.Vector2 position,
            out bool isDead)
        {
            isDead = false;
            return false;
        }

        public void HitBack(UnityEngine.Vector2 position) =>
            HitBackCount++;
    }
}

namespace DolocTown.UI
{
    public sealed class AccessoriesBar
    {
        public void __Init()
        {
        }

        public void OnStartShow()
        {
        }
    }
}
