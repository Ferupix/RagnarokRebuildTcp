using RebuildSharedData.Enum;
using RoRebuildServer.Data;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.EntityComponents.Npcs;
using RoRebuildServer.Simulation.Items;
using RoRebuildServer.Simulation.Skills.SkillHandlers.Mage;
using RoRebuildServer.Simulation.Skills.SkillHandlers.Wizard;
using RoRebuildServer.Simulation.Util;

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
                    using (var targetList = EntityListPool.Get())
                    {
                        // ToDo: Placeholder for Storm Gust = Inneficient command FrostDiver
                        // ToDo: change first param when NPCs have a CombatEntity
                        src.Character.Map!.GatherEnemiesInArea(src.Character, npc.SelfPosition, 3, targetList, false, true);

                        foreach (var e in targetList)
                        {
                            FrostDiverHandler ice = new FrostDiverHandler();
                            ice.Process(src, e.Get<CombatEntity>(), 10, true);
                        }
                    }
                      
                    break;
                case 2:
                    ThunderStormHandler thunder = new ThunderStormHandler();
                    thunder.Process(src, null!, npc.SelfPosition, 1, true, false);
                    break;
                case 3:
                    HeavensDriveHandler earth = new HeavensDriveHandler();
                    earth.Process(src, null!, npc.SelfPosition, 1, true, false);
                    break;
                case 4:
                    using (var targetList = EntityListPool.Get())
                    {
                        // ToDo: Placeholder for Meteor Storm = Inneficient command Fireball (also, laggy)
                        // ToDo: change first param when NPCs have a CombatEntity
                        src.Character.Map!.GatherEnemiesInArea(src.Character, npc.SelfPosition, 3, targetList, false, true);

                        foreach (var e in targetList)
                        {
                            FireBallHandler fire = new FireBallHandler();
                            fire.Process(src, e.Get<CombatEntity>(), 10, true);
                        }
                    }
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
