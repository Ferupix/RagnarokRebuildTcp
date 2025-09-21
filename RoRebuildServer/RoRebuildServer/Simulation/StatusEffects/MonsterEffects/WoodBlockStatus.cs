using RebuildSharedData.Enum;
using RoRebuildServer.EntityComponents.Character;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.Simulation.StatusEffects.Setup;
using RoRebuildServer.EntityComponents.Util;

namespace RoRebuildServer.Simulation.StatusEffects.MonsterEffects
{
    [StatusEffectHandler(CharacterStatusEffect.WoodBlock1, StatusClientVisibility.Everyone, StatusEffectFlags.NoSave)]
    public class WoodBlockStatus : StatusEffectBase
    {
        public override StatusUpdateMode UpdateMode => StatusUpdateMode.OnTakeDamage;

        public override void OnExpiration(CombatEntity ch, ref StatusEffectState state)
        {
            // Whenever the status effect expires, reapply the next status to the entity
            ch.AddStatusEffect(StatusEffectState.NewStatusEffect(CharacterStatusEffect.WoodBlock2, int.MaxValue));
        }

        public override StatusUpdateResult OnTakeDamage(CombatEntity ch, ref StatusEffectState state, ref DamageInfo info)
        {
            if (info.IsDamageResult && info.Damage > 0)
            {
                // Remove status effect as soon as the entity takes damage
                return StatusUpdateResult.EndStatus;
            }

            return StatusUpdateResult.Continue;
        }
    }

    [StatusEffectHandler(CharacterStatusEffect.WoodBlock2, StatusClientVisibility.Everyone, StatusEffectFlags.NoSave)]
    public class WoodBlock2Status : StatusEffectBase
    {
        public override StatusUpdateMode UpdateMode => StatusUpdateMode.OnTakeDamage;

        public override void OnExpiration(CombatEntity ch, ref StatusEffectState state)
        {
            ch.AddStatusEffect(StatusEffectState.NewStatusEffect(CharacterStatusEffect.WoodBlock3, int.MaxValue));
        }

        public override StatusUpdateResult OnTakeDamage(CombatEntity ch, ref StatusEffectState state, ref DamageInfo info)
        {
            if (info.IsDamageResult && info.Damage > 0)
            {
                return StatusUpdateResult.EndStatus;
            }

            return StatusUpdateResult.Continue;
        }
    }

    [StatusEffectHandler(CharacterStatusEffect.WoodBlock3, StatusClientVisibility.Everyone, StatusEffectFlags.NoSave)]
    public class WoodBlock3Status : StatusEffectBase
    {
        public override StatusUpdateMode UpdateMode => StatusUpdateMode.OnTakeDamage;

        public override void OnExpiration(CombatEntity ch, ref StatusEffectState state)
        {
            // ToDo: Find which statue we are building and set unique woodBlock4_statueName status (unique model for different statues)
            // ch.AddStatusEffect(StatusEffectState.NewStatusEffect(CharacterStatusEffect.WoodBlock4, int.MaxValue)); 
        }

        public override StatusUpdateResult OnTakeDamage(CombatEntity ch, ref StatusEffectState state, ref DamageInfo info)
        {
            if (info.IsDamageResult && info.Damage > 0)
            {
                return StatusUpdateResult.EndStatus;
            }

            return StatusUpdateResult.Continue;
        }
    }
}
