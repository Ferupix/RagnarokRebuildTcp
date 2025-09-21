using Assets.Scripts.Network;
using RebuildSharedData.Enum;

namespace Assets.Scripts.SkillHandlers.Handlers._ToS.Dievdirbys
{
    [SkillHandler(CharacterSkill.StatueOfGoddessCarveOwl)]
    public class StatueOfGoddessCarveOwlHandler : SkillHandlerBase
    {
        public override void ExecuteSkillGroundTargeted(ServerControllable src, ref AttackResultData attack)
        {
            src.PerformSkillMotion();
            if(src.CharacterType == CharacterType.Player)
                src.FloatingDisplay.ShowChatBubbleMessage("Carve Owl!!");
        }
    }
}
