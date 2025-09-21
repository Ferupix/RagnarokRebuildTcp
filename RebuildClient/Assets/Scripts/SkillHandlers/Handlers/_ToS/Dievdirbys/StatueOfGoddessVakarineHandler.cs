using Assets.Scripts.Network;
using RebuildSharedData.Enum;
using UnityEngine;

namespace Assets.Scripts.SkillHandlers.Handlers._ToS.Dievdirbys
{
    [SkillHandler(CharacterSkill.StatueOfGoddessVakarine)]
    public class StatueOfGoddessVakarineHandler : SkillHandlerBase
    {
        public override void ExecuteSkillGroundTargeted(ServerControllable src, ref AttackResultData attack)
        {
            src.PerformSkillMotion();
            if(src.CharacterType == CharacterType.Player)
                src.FloatingDisplay.ShowChatBubbleMessage("Statue of Goddess Vakarine!!");
        }

        // public override void StartSkillCasting(ServerControllable src, Vector2Int target, int lvl, float castTime)
        // {
        //     HoldAttackMotionForCast(src, castTime);
        // }
    }
}
