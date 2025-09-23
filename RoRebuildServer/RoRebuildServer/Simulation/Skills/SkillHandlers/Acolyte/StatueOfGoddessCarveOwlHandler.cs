using RebuildSharedData.Enum;
using RoRebuildServer.Data;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.EntityComponents.Npcs;
using RoRebuildServer.Simulation.Items;
using RoRebuildServer.Simulation.Skills.SkillHandlers.Mage;
using RoRebuildServer.Simulation.Skills.SkillHandlers.Wizard;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Acolyte
{
    [MonsterSkillHandler(CharacterSkill.StatueOfGoddessCarveOwl, SkillClass.Physical, SkillTarget.Ground)]
    [SkillHandler(CharacterSkill.StatueOfGoddessCarveOwl, SkillClass.Physical, SkillTarget.Ground)]
    public class StatueOfGoddessCarveOwlHandler : SkillHandlerStatueOfGoddess
    {
        protected override string GroundUnitType() => nameof(StatueOfGoddessCarveOwlEvent);
        protected override CharacterSkill SkillType() => CharacterSkill.StatueOfGoddessCarveOwl;
        public override int GetSkillRange(CombatEntity source, int lvl) => 1;
        protected override int Catalyst() => 1019; // Trunk
    }

    public class StatueOfGoddessCarveOwlEvent : StatueOfGoddessBaseEvent
    {
        protected override CharacterSkill SkillSource() => CharacterSkill.StatueOfGoddessCarveOwl;
        protected override NpcEffectType EffectType() => NpcEffectType.StatueOfGoddessCarveOwl;

        protected override float Duration(int skillLevel) => 30f; // ToDo: review duration
        protected override bool AllowAutoAttackMove => true;

        public override void OnNaturalExpiration(Npc npc)
        {
            if (npc.Owner.TryGet<WorldObject>(out var owner) && owner.Type != CharacterType.Player)
                return;

            var item = new GroundItem(npc.Character.Position, 1019, 1);
            npc.Character.Map?.DropGroundItem(ref item);
        }

        public override bool TriggerStatue(Npc npc, CombatEntity src, CombatEntity? target, int skillLevel)
        {
            switch (npc.ValuesInt[1])
            {
                case 1:
                    HeavensDriveHandler earth = new HeavensDriveHandler();
                    earth.Process(src, null!, npc.SelfPosition, 1, true, false); // ToDo: Animation doesn't play while player is moving
                    break;
                case 2:
                    ThunderStormHandler thunder = new ThunderStormHandler();
                    thunder.Process(src, null!, npc.SelfPosition, 1, true, false);
                    break;
                case 3:
                    // ToDo: Placeholder for Storm Gust (1cell)
                    break;
                case 4:
                    // ToDo: Placeholder for Meteor Storm (1cell)
                    break;
            }

            return true;
        }
    }

    public class NpcLoaderStatueOfGoddessCarveOwl : INpcLoader
    {
        public void Load()
        {
            DataManager.RegisterEvent(nameof(StatueOfGoddessCarveOwlEvent), new StatueOfGoddessCarveOwlEvent());
        }
    }
}
