using RebuildSharedData.Enum;
using RoRebuildServer.Data;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.EntityComponents.Npcs;
using RoRebuildServer.EntityComponents.Util;
using RoRebuildServer.Logging;
using RoRebuildServer.Simulation.Items;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Acolyte
{
    [MonsterSkillHandler(CharacterSkill.StatueOfGoddessVakarine, SkillClass.Physical, SkillTarget.Ground)]
    [SkillHandler(CharacterSkill.StatueOfGoddessVakarine, SkillClass.Physical, SkillTarget.Ground)]
    public class StatueOfGoddessVakarineHandler : SkillHandlerStatueOfGoddess
    {
        protected override string GroundUnitType() => nameof(StatueOfGoddessVakarineEvent);
        protected override CharacterSkill SkillType() => CharacterSkill.StatueOfGoddessVakarine;
        public override int GetSkillRange(CombatEntity source, int lvl) => 1;
        protected override int Catalyst() => 1019; // Trunk
    }

    public class StatueOfGoddessVakarineEvent : StatueOfGoddessBaseEvent
    {
        protected override CharacterSkill SkillSource() => CharacterSkill.StatueOfGoddessVakarine;
        protected override NpcEffectType EffectType() => NpcEffectType.StatueOfGoddessVakarine;

        protected override float Duration(int skillLevel) => 180f; // ToDo: review duration

        public override void OnNaturalExpiration(Npc npc)
        {
            if (npc.Owner.TryGet<WorldObject>(out var owner) && owner.Type != CharacterType.Player)
                return;

            var item = new GroundItem(npc.Character.Position, 1019, 1);
            npc.Character.Map?.DropGroundItem(ref item);
        }

        public override bool TriggerStatue(Npc npc, CombatEntity src, CombatEntity? target, int skillLevel)
        {
            if (target!.Character.Type != CharacterType.Player)
                return false;
            if (target.Character.IsMoving || target.Character.IsTargetImmune || target.Character.State == CharacterState.Dead)
                return false;
            if (string.IsNullOrWhiteSpace(npc.ValuesString[0]))
                return false;

            var p = target.Player;
            p.Character.StopMovingImmediately();
            p.ClearTarget();
            target.CancelCast();

            WorldObject owner;
            npc.Owner.TryGet<WorldObject>(out owner);

            // ToDo: Instead of predefined xy positions, access player-statue-tracker array?
            AreaOfEffect? previousStatueAoe;
            if(owner != null && owner.Map!.TryGetAreaOfEffectAtPosition(new RebuildSharedData.Data.Position(npc.ValuesInt[1], npc.ValuesInt[2]), CharacterSkill.StatueOfGoddessVakarine, out previousStatueAoe))
            {
                if(previousStatueAoe.SourceEntity.Id == src.Entity.Id)
                {
                    // ToDo: Find Player Id and compare with statue id? Party id? Maybe not required, since WarpPortal-griefing by standing in the Cell is possible
                }

                if (!p.WarpPlayer(npc.ValuesString[0], npc.ValuesInt[1], npc.ValuesInt[2], 1, 1, false))
                {
                    ServerLogger.LogWarning($"Failed to move player to {npc.ValuesString[0]}!");
                    return false;
                }
            }

            npc.ValuesInt[3] += 1;

            return true;
        }
    }

    public class NpcLoaderStatueOfGoddess : INpcLoader
    {
        public void Load()
        {
            DataManager.RegisterEvent(nameof(StatueOfGoddessVakarineEvent), new StatueOfGoddessVakarineEvent());
        }
    }
}
