namespace DTMAPI.MoreEquipmentSlots
{
    internal sealed class MoreEquipmentSlotsNativeShieldAdapter :
        DolocTown.IAgentEquipmentShieldItem
    {
        private readonly MoreEquipmentSlotsNativeRuntime runtime;

        internal MoreEquipmentSlotsNativeShieldAdapter(
            MoreEquipmentSlotsNativeRuntime runtime) =>
            this.runtime = runtime ??
                throw new System.ArgumentNullException(
                    nameof(runtime));

        public float ShieldPercent =>
            runtime.GetNativeShieldPercent();

        public float ShieldValue =>
            runtime.GetNativeShieldValue();

        public bool TryBlockAttack(
            int damage,
            out int blockedDamage) =>
            runtime.TryBlockNativeAttack(
                damage,
                out blockedDamage);
    }
}
