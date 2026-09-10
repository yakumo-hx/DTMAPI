public static class DolocAPI
{
    public static int HideHoverBoxCallCount;

    public static object? archiveHandle =
        new FixtureArchiveHandle();
    public static object? dataPersistenceManager;
    public static bool BackpackPlacementAvailable = true;
    public static System.Func<string, int, bool, bool>?
        TryPlaceInBackpackOverride;
    public static int TryPlaceInBackpackCallCount;
    public static System.Func<string, bool, int>?
        CountItemOverride;
    public static int CountItemOverrideCallCount;
    public static bool CostItemThrowAfterMutation;
    public static int CostItemCallCount;
    public static bool NativeMutationObservationPending;
    public static bool ThrowOnNativeMutationObservation;
    public static int NativeMutationObservationCallCount;
    public static System.Func<string, int, bool>?
        SendItemAsEmailOverride;
    public static int SendItemAsEmailCallCount;
    public static DolocTown.HurtReason LastHurtReason;
    public static int LastHealthCost;
    public static bool CostHealthReturnsDead;
    public static int DamageTipCount;
    public static int HurtBroadcastCount;
    public static int LastDamageTip;
    public static bool LastDamageTipHeavy;
    public static DolocTown.GameData.AgentEquipmentManager?
        AgentEquipmentManager;
    public static FixtureUiSystem uiSystem { get; } =
        new FixtureUiSystem();
    public static FixtureAgentController AgentController { get; } =
        new FixtureAgentController();
    public static FixtureGlobalParameter GlobalParameter { get; } =
        new FixtureGlobalParameter();

    public static void HideHoverBox() =>
        HideHoverBoxCallCount++;

    public static void HideHoverBox(object owner) =>
        HideHoverBoxCallCount++;

    public static void HideHoverBox(
        UnityEngine.RectTransform owner) =>
        HideHoverBoxCallCount++;

    private static readonly System.Collections.Generic.Dictionary<string, int>
        Backpack =
            new System.Collections.Generic.Dictionary<string, int>(
                System.StringComparer.Ordinal);

    public static int CountItem(string itemId, bool includeEquipment)
    {
        if (NativeMutationObservationPending)
        {
            NativeMutationObservationPending = false;
            NativeMutationObservationCallCount++;
            if (ThrowOnNativeMutationObservation)
            {
                throw new System.InvalidOperationException(
                    "fixture immediate native mutation observation failure");
            }
        }
        if (CountItemOverride != null)
        {
            CountItemOverrideCallCount++;
            return CountItemOverride(itemId, includeEquipment);
        }
        return GetStoredItemCount(itemId);
    }

    public static int GetStoredItemCount(string itemId) =>
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
        TryPlaceInBackpackCallCount++;
        if (TryPlaceInBackpackOverride != null)
        {
            return TryPlaceInBackpackOverride(
                itemId,
                count,
                sendByMail);
        }
        if (!CanPlaceItem(itemId, count))
            return false;
        Backpack[itemId] = GetStoredItemCount(itemId) + count;
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
        CostItemCallCount++;
        int current = CountItem(itemId, includeEquipment);
        if (string.IsNullOrWhiteSpace(itemId) ||
            count <= 0 ||
            current < count)
        {
            return false;
        }
        Backpack[itemId] = current - count;
        if (CostItemThrowAfterMutation)
        {
            throw new System.InvalidOperationException(
                "fixture CostItem mutate-then-throw");
        }
        return true;
    }

    public static void ResetInventory()
    {
        Backpack.Clear();
        archiveHandle = new FixtureArchiveHandle();
        dataPersistenceManager = null;
        BackpackPlacementAvailable = true;
        TryPlaceInBackpackOverride = null;
        TryPlaceInBackpackCallCount = 0;
        CountItemOverride = null;
        CountItemOverrideCallCount = 0;
        CostItemThrowAfterMutation = false;
        CostItemCallCount = 0;
        NativeMutationObservationPending = false;
        ThrowOnNativeMutationObservation = false;
        NativeMutationObservationCallCount = 0;
        SendItemAsEmailOverride = null;
        SendItemAsEmailCallCount = 0;
        HideHoverBoxCallCount = 0;
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
        float scale)
    {
        DamageTipCount++;
        LastDamageTip = damage;
        LastDamageTipHeavy = critical;
    }

    public static void RaiseDamageTip(
        int damage,
        UnityEngine.Vector2 position,
        bool isHeavy,
        float duration,
        float height,
        float scale,
        int style)
    {
        DamageTipCount++;
        LastDamageTip = damage;
        LastDamageTipHeavy = isHeavy;
    }

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
        LastDamageTip = 0;
        LastDamageTipHeavy = false;
        AgentController.droneController.EscapeCombatCount = 0;
        uiSystem.agentStatusBar.UpdateHealthCount = 0;
        DolocTown.AgentStateFishing.UnsetUiControlCount = 0;
    }
}

public sealed class FixtureUiSystem
{
    public FixtureAgentStatusBar agentStatusBar { get; } =
        new FixtureAgentStatusBar();
}

public sealed class FixtureAgentStatusBar
{
    public int UpdateHealthCount { get; set; }

    public void UpdateHealth() =>
        UpdateHealthCount++;
}

public sealed class FixtureAgentController
{
    public FixtureDroneController droneController { get; } =
        new FixtureDroneController();
}

public sealed class FixtureGlobalParameter
{
    public float CriticalDamageRate { get; set; } = 2f;
}

public sealed class FixtureDroneController
{
    public int EscapeCombatCount { get; set; }

    public void EscapeCombat() =>
        EscapeCombatCount++;
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
        public AgentEquipmentManager() =>
            DolocAPI.AgentEquipmentManager = this;

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

        public DolocTown.IAgentEquipmentShieldItem?
            ShieldOverride { get; set; }

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

        public bool TryGetShieldItem(
            out DolocTown.IAgentEquipmentShieldItem? item)
        {
            if (ThrowOnShieldProbe)
            {
                throw new System.InvalidOperationException(
                    "fixture native shield probe failure");
            }
            item = ShieldOverride ??
                (HasNativeShield
                    ? new DolocTown.FixtureNativeShield()
                    : null);
            return item != null;
        }
    }
}

namespace DolocTown
{
    public interface IAgentEquipmentShieldItem
    {
        float ShieldPercent { get; }

        float ShieldValue { get; }

        bool TryBlockAttack(
            int damage,
            out int blockedDamage);
    }

    public sealed class FixtureNativeShield :
        IAgentEquipmentShieldItem
    {
        public float ShieldPercent => 1f;

        public float ShieldValue => 100f;

        public bool TryBlockAttack(
            int damage,
            out int blockedDamage)
        {
            blockedDamage = damage;
            return true;
        }
    }

    public sealed class EmailAttachReward
    {
        public bool isAccept { get; set; }

        public object reward { get; set; } =
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

    public class ItemPassive : Item
    {
    }

    public sealed class FixtureItemHatOrdinary : ItemHat
    {
        public FixtureItemHatOrdinary()
        {
            proto.Title = "fixture ordinary hat";
            proto.Function =
                new DolocTown.Config.Item.ItemFunctionHat();
        }
    }

    public sealed class FixtureItemPassive : ItemPassive
    {
        public FixtureItemPassive(object function)
        {
            proto.Title = "fixture passive equipment";
            proto.Function = function;
        }
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
        public object Function
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
                : string.Equals(
                    itemId,
                    "fixture-hat",
                    System.StringComparison.Ordinal)
                    ? new FixtureItemHatOrdinary()
                    : string.Equals(
                        itemId,
                        "fixture-passive",
                        System.StringComparison.Ordinal)
                        ? new FixtureItemPassive(
                            new Config.Item.ItemFunctionPassive())
                        : string.Equals(
                            itemId,
                            "fixture-herb-package",
                            System.StringComparison.Ordinal)
                            ? new FixtureItemPassive(
                                new Config.Item.ItemFunctionHerbPackage())
                            : string.Equals(
                                itemId,
                                "fixture-skillless-passive",
                                System.StringComparison.Ordinal)
                                ? new FixtureItemPassive(
                                    new Config.Item.ItemFunctionPassive
                                    {
                                        Skill = string.Empty
                                    })
                                : string.Equals(
                                    itemId,
                                    "fixture-active",
                                    System.StringComparison.Ordinal)
                                    ? new Item
                                    {
                                        proto =
                                            new FixtureItemProto
                                            {
                                                Title =
                                                    "fixture active",
                                                Function =
                                                    new Config
                                                        .Item
                                                        .ItemFunctionActive()
                                            }
                                    }
                                    : null;
            if (item?.proto.Function is
                FixtureItemFunctionHatShield shield)
            {
                shield.MaxShieldValue = ShieldMaxValue;
            }
            return item != null;
        }
    }

    namespace Config.Item
    {
        public sealed class ItemFunctionHat
        {
            public FixtureHatInfo HatId_Ref { get; } =
                new FixtureHatInfo
                {
                    Skill = string.Empty,
                    Defense = 0,
                    Skill_Ref =
                        new FixtureSkillInfo
                        {
                            Function = new object()
                        }
                };
        }

        public sealed class ItemFunctionPassive
        {
            public string Skill { get; set; } =
                "fixture-passive-skill";
        }

        public sealed class ItemFunctionHerbPackage
        {
            public string Skill { get; set; } =
                "fixture-herb-skill";
        }

        public sealed class ItemFunctionActive
        {
            public string Skill { get; set; } =
                "fixture-active-skill";
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

    public enum AttackableType
    {
        None = 0,
        Player = 1,
        Unattackable = 2
    }

    public enum AttackPropertyType
    {
        Normal = 0,
        Thunder = 1
    }

    public sealed class AttackProperties
    {
        public static AttackProperties Normal { get; } =
            new AttackProperties();

        public static AttackProperties Physical { get; } =
            new AttackProperties();

        public AttackPropertyType attackType { get; set; } =
            AttackPropertyType.Normal;
    }

    public static class BattleUtils
    {
        public static int CalcDamage(
            float attack,
            float defend,
            bool critical) =>
            System.Math.Max(
                (int)System.Math.Round(
                    (critical ? attack * 2f : attack) -
                    defend),
                1);
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
        private AttackableType _attackableType =
            AttackableType.Player;

        public bool IsFaint { get; set; }

        public float CurrentDefend { get; set; }

        public FixtureStateManager StateManager { get; } =
            new FixtureStateManager();

        public FixtureHatRenderer HatRenderer { get; } =
            new FixtureHatRenderer();

        public FixtureFishRodRenderer fishRodRenderer { get; } =
            new FixtureFishRodRenderer();

        public int HitBackCount { get; private set; }

        public int InvincibilityStartCount { get; private set; }

        public int InvincibilityRestoreCount { get; private set; }

        public int InvincibilityDuration => 1000;

        public AttackableType AttackableType =>
            _attackableType;

        public bool OnAttacked(
            float attack,
            bool isCritical,
            UnityEngine.Vector2 position,
            AttackProperties properties,
            out bool isDead)
        {
            isDead = false;
            if (_attackableType ==
                    AttackableType.Unattackable ||
                IsFaint)
            {
                return false;
            }

            int damage = BattleUtils.CalcDamage(
                attack,
                CurrentDefend,
                isCritical);
            if (DolocAPI.AgentEquipmentManager != null &&
                DolocAPI.AgentEquipmentManager
                    .TryGetShieldItem(out var shield) &&
                shield != null)
            {
                if (shield.TryBlockAttack(
                        damage,
                        out int blockedDamage))
                {
                    DolocAPI.RaiseDamageTip(
                        0,
                        position,
                        false,
                        0.7f,
                        50f,
                        1.5f);
                    HatRenderer.Shine();
                    HitBack(position);
                    return true;
                }
                damage -= blockedDamage;
            }

            if (properties?.attackType ==
                AttackPropertyType.Thunder)
            {
                DolocAPI.RaiseDamageTip(
                    damage,
                    position,
                    true,
                    0.7f,
                    50f,
                    1.5f,
                    1);
            }
            else
            {
                DolocAPI.RaiseDamageTip(
                    damage,
                    position,
                    isCritical,
                    0.7f,
                    50f,
                    1.5f);
            }
            DolocAPI.Broadcast(
                GameEventType.HURT_BY_MONSTER);
            if (DolocAPI.CostHealth(
                    damage,
                    HurtReason.MonsterAttack))
            {
                isDead = true;
                DolocAPI.AgentController
                    .droneController
                    .EscapeCombat();
            }
            else
            {
                SetAttackable(false);
                if (StateManager.current is AgentStateFishing)
                {
                    AgentStateFishing.UnsetUiControl();
                    fishRodRenderer.SetVisible(false);
                }
                StateManager.Overwrite<AgentStateHit>();
            }
            HitBack(position);
            return true;
        }

        public void SetAttackable(bool value)
        {
            _attackableType = value
                ? AttackableType.Player
                : AttackableType.Unattackable;
            if (value)
                InvincibilityRestoreCount++;
            else
                InvincibilityStartCount++;
        }

        public void HitBack(UnityEngine.Vector2 position) =>
            HitBackCount++;
    }
}

namespace Cysharp.Threading.Tasks
{
    public enum PlayerLoopTiming
    {
        Update = 8
    }

    public readonly struct UniTask
    {
        public static UniTask Delay(
            int millisecondsDelay,
            bool ignoreTimeScale,
            PlayerLoopTiming timing,
            System.Threading.CancellationToken cancellationToken) =>
            new UniTask();
    }

    public static class UniTaskExtensions
    {
        public static UniTask ContinueWith(
            UniTask task,
            System.Action continuation)
        {
            continuation();
            return new UniTask();
        }

        public static void Forget(UniTask task)
        {
        }
    }
}
