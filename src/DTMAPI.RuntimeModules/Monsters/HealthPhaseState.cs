using System;
using System.Collections.Generic;
namespace DTMAPI.MonsterContent
{
    internal sealed class HealthPhaseState
    {
        private readonly MonsterHealthPhases plan;
        internal int Index { get; private set; }
        internal int ReachedIndex { get; private set; }
        internal int Revision { get; private set; }
        internal bool Pending => ReachedIndex > Index;
        internal HealthPhaseState(MonsterHealthPhases plan) { this.plan = plan; }
        internal float Speed => Index == 0 ? 1 : plan.Stages[Index - 1].SpeedMultiplier;
        internal float Damage => Index == 0 ? 1 : plan.Stages[Index - 1].DamageMultiplier;
        internal void Observe(int health, int maximum)
        {
            if (health <= 0 || maximum <= 0) return;
            while (ReachedIndex < plan.Stages.Count && (float)health / maximum <= plan.Stages[ReachedIndex].AtOrBelow) ReachedIndex++;
        }
        internal bool Commit(int health, bool actionRunning)
        {
            if (health <= 0 || actionRunning || !Pending) return false;
            Index = ReachedIndex; Revision++; return true;
        }
        internal MonsterHealthPhaseSnapshot Snapshot() => new MonsterHealthPhaseSnapshot(Index, ReachedIndex,
            Index == 0 ? "" : plan.Stages[Index - 1].Id, Pending ? plan.Stages[ReachedIndex - 1].Id : "", Revision, Speed, Damage);
        internal MonsterScatter Scale(MonsterScatter source) { var p = source.Copy(); p.Damage = source.Damage * Damage; return p; }
        internal MonsterDash Scale(MonsterDash source) { var p = source.Copy(); p.Damage = source.Damage * Damage; p.Speed = source.Speed * Speed; return p; }
    }
}
