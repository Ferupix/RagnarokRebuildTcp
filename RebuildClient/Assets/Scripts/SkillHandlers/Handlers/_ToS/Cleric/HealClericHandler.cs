using Assets.Scripts.Effects.EffectHandlers;
using Assets.Scripts.Network;
using Assets.Scripts.Objects;
using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;
using UnityEngine;

namespace Assets.Scripts.SkillHandlers.Handlers
{
    [SkillHandler(CharacterSkill.HealCleric)]
    public class HealClericHandler : SkillHandlerBase
    {
        public override void StartSkillCasting(ServerControllable src, Vector2Int target, int lvl, float castTime)
        {
            HoldStandbyMotionForCast(src, castTime);
            src.AttachEffect(CastEffect.Create(castTime, src.gameObject, AttackElement.Holy));
            
            var targetCell = CameraFollower.Instance.WalkProvider.GetWorldPositionForTile(target);
            if(target != Vector2Int.zero)
                CastTargetCircle.Create(src.IsAlly, targetCell, 3, castTime);
        }
        
        public override void StartSkillCasting(ServerControllable src, ServerControllable target, int lvl, float castTime)
        {
            HoldStandbyMotionForCast(src, castTime);
            src.AttachEffect(CastEffect.Create(castTime, src.gameObject, AttackElement.Holy));
            
            if(target != null)
                target.AttachEffect(CastLockOnEffect.Create(castTime, target.gameObject));
        }

        public override void ExecuteSkillGroundTargeted(ServerControllable src, ref AttackResultData attack)
        {
            src.PerformSkillMotion();
            if(src.CharacterType == CharacterType.Player)
                src.FloatingDisplay.ShowChatBubbleMessage("Heal (Cleric)!!");

            CameraFollower.Instance.CreateEffectAtLocation("HealCleric", attack.TargetAoE.ToWorldPosition(), new Vector3(1.5f, 1.5f, 1.5f), 0);

            string healSkillVoiceFile = Random.Range(1, 3) switch
            {
                1 => "_ToS/skillvoice_kor/skillvoice/voice_cleric_heal_shot_f_1.wav",
                2 => "_ToS/skillvoice_kor/skillvoice/voice_cleric_heal_shot_f_2.wav",
                3 => "_ToS/skillvoice_kor/skillvoice/voice_cleric_heal_shot_f_3.wav",
                _ => "_ToS/skillvoice_kor/skillvoice/voice_cleric_heal_shot_f_1.wav"
            };

            // Play skillvoice "Heal!"
            AudioManager.Instance.OneShotSoundEffect(src.Id, healSkillVoiceFile, attack.TargetAoE.ToWorldPosition());

            // Play skill effect for heal
            AudioManager.Instance.OneShotSoundEffect(src.Id, $"_ToS/skillvoice_kor/SE/skl_cleric_heal_shot.wav", attack.TargetAoE.ToWorldPosition());
        }

        public override void OnHitEffect(ServerControllable target, ref AttackResultData attack)
        {
            target.Messages.SendElementalHitEffect(attack.Src, attack.DamageTiming, AttackElement.Holy, attack.HitCount);
        }
    }
}
