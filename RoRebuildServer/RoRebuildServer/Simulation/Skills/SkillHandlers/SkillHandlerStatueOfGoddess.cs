using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.EntityComponents.Character;
using RoRebuildServer.EntityComponents.Npcs;
using RoRebuildServer.EntityComponents.Util;
using RoRebuildServer.Logging;
using RoRebuildServer.Networking;
using RoRebuildServer.Simulation.StatusEffects.Setup;
using System.Diagnostics;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers
{
    public abstract class SkillHandlerStatueOfGoddess : SkillHandlerBase
    {
        protected abstract string GroundUnitType();
        protected abstract CharacterSkill SkillType();
        protected virtual int Catalyst() => -1;
        protected virtual int CatalystCount() => 1;

        public override SkillValidationResult ValidateTarget(CombatEntity source, CombatEntity? target, Position position,
            int lvl, bool isIndirect, bool isItemSource)
        {
            if (source.Character.Type == CharacterType.Player)
            {
                var item = Catalyst();
                if (item > 0)
                {
                    var count = CatalystCount();
                    if (source.Character.Type == CharacterType.Player && (source.Player.Inventory == null || source.Player.Inventory.GetItemCount(item) < count))
                        return SkillValidationResult.MissingRequiredItem;
                }
            }

            return base.ValidateTarget(source, target, position, lvl, false, false);
        }

        //pre-validation occurs after the cast bar and is the last chance for a skill to fail.
        //Default validation will make sure we have LoS and the cell is valid
        public override bool PreProcessValidation(CombatEntity source, CombatEntity? target, Position position, int lvl,
            bool isIndirect, bool isItemSource)
        {
            var map = source.Character.Map;
            Debug.Assert(map != null);

            //we check the cell here because it could have changed since regular validation via ice wall, script, etc.
            if (!map.WalkData.IsCellWalkable(position))
            {
                if (source.Character.Type == CharacterType.Player)
                    CommandBuilder.SkillFailed(source.Player, SkillValidationResult.Failure);
                return false;
            }

            var distance = 0;

            var effectiveArea = Area.CreateAroundPoint(position, distance);
            if (map.DoesAreaOverlapWithTrapsOrCharacters(effectiveArea))
            {
                if (source.Character.Type == CharacterType.Player)
                    CommandBuilder.SkillFailed(source.Player, SkillValidationResult.TargetAreaOccupied);
                return false;
            }

            return true;
        }

        public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl, bool isIndirect,
            bool isItemSource)
        {
            var map = source.Character.Map;
            Debug.Assert(map != null);

            if (source.Character.Type == CharacterType.Player && !source.Player.TryRemoveItemFromInventory(Catalyst(), CatalystCount(), true))
                return;

            var ch = source.Character;

            int param1 = 0, param2 = 0, param3 = 0, param4 = 0;
            string paramString = string.Empty;

            switch (SkillType())
            {
                case CharacterSkill.StatueOfGoddessVakarine:
                    // ToDo: Get previous statue pos (first statue doesn't know the other statue pos)
                    var previousStatues = source.Player.SpawnedAoes!.Where(x => x.Key == CharacterSkill.StatueOfGoddessVakarine);
                    KeyValuePair<CharacterSkill, Position>? prevStatue = previousStatues.Count() > 0 ? previousStatues.Last() : null;

                    int prevX = -1, prevY = -1;
                    if (prevStatue.HasValue)
                    {
                        prevX = prevStatue.Value.Value.X;
                        prevY = prevStatue.Value.Value.Y;
                    }

                    param1 = lvl;
                    param2 = prevX;
                    param3 = prevY;
                    param4 = 0;
                    paramString = map.Name;

                    source.Player.SpawnedAoes!.Add(new KeyValuePair<CharacterSkill, Position>(CharacterSkill.StatueOfGoddessVakarine, position)); // ToDo: Find a better way to store statue pos
                    break;
                case CharacterSkill.StatueOfGoddessCarveOwl:
                    // ToDo: params
                    param1 = lvl;
                    break;
            }

            var e = World.Instance.CreateEvent(source.Entity, map, GroundUnitType(), position, param1, param2, param3, param4, paramString, true);

            BattleNpc evBattleNpc = e.Get<BattleNpc>();
            evBattleNpc.Character.CombatEntity.AddStatusEffect(StatusEffectState.NewStatusEffect(CharacterStatusEffect.WoodBlock1, int.MaxValue));

            ch.AttachEvent(e);
            source.ApplyCooldownForSupportSkillAction();

            if (!isIndirect)
                CommandBuilder.SkillExecuteAreaTargetedSkillAutoVis(ch, position, SkillType(), lvl);
        }
    }

    public abstract class StatueOfGoddessBaseEvent : NpcBehaviorBase
    {
        protected abstract CharacterSkill SkillSource();
        protected abstract NpcEffectType EffectType();
        protected abstract float Duration(int skillLevel);
        protected virtual bool Attackable(Npc npc) { return npc.EffectType == NpcEffectType.StatueOfGoddessWoodBlock; }
        protected virtual bool AllowAutoAttackMove => false;

        public override void InitEvent(Npc npc, int param1, int param2, int param3, int param4, string? paramString)
        {
            if (npc.Character.Type != CharacterType.BattleNpc)
                throw new Exception($"Cannot create StatueOfGoddessBaseEvent npc as it is not correctly assigned as a BattleNPC type.");

            //if (paramString == null)
            //    ServerLogger.LogErrorWithStackTrace($"Attempting to create StatueOfGoddessBaseEvent without a string parameter!");

            npc.RevealAsEffect(NpcEffectType.StatueOfGoddessWoodBlock, "StatueOfGoddessWoodBlock");
            npc.ValuesInt[0] = param1; // skill level
            npc.ValuesInt[1] = param2; // xPos for Vaka, skillRotationIndex for Owl
            npc.ValuesInt[2] = param3; // yPos for Vaka //ToDo: why 0
            npc.ValuesInt[3] = param4; // usage counter for Vaka
            npc.ValuesInt[4] = 0;
            npc.ValuesInt[5] = 0;
            npc.ValuesString[0] = paramString!; // mapName for Vaka

            if (!npc.Owner.TryGet<WorldObject>(out var owner))
            {
                ServerLogger.LogWarning($"Npc {npc.Character} running StatueOfGoddessBaseEvent init but does not have an owner.");
                return;
            }

            // Initialize the wood block CE
            var ce = npc.Character.CombatEntity;
            ce.SetStat(CharacterStat.MaxHp, 10);
            ce.SetStat(CharacterStat.Hp, 10);

            TargetingInfo targeting = new TargetingInfo()
            {
                Faction = owner.Type == CharacterType.Player ? 1 : 0,
                Party = 0,
                IsPvp = false,
                SourceEntity = npc.Owner
            };

            int distance = 0;
            float tickRate = 1f;

            switch (SkillSource())
            {
                case CharacterSkill.StatueOfGoddessVakarine:
                    targeting.TargetingType = TargetingType.Player; // ToDo: test using Party
                    tickRate = 1f;
                    break;
                case CharacterSkill.StatueOfGoddessCarveOwl:
                    targeting.TargetingType = TargetingType.Enemies;
                    npc.ValuesInt[1] = 1; // Initialize with the first skill to be cast by Owl
                    break;
            }

            var aoe = World.Instance.GetNewAreaOfEffect();
            aoe.Init(npc.Character, Area.CreateAroundPoint(npc.Character.Position, distance), AoeType.SpecialEffect, targeting, Duration(npc.ValuesInt[0]), tickRate, 0, 0);
            aoe.TriggerOnFirstTouch = SkillSource() == CharacterSkill.StatueOfGoddessVakarine;
            aoe.CheckStayTouching = SkillSource() == CharacterSkill.StatueOfGoddessVakarine; // Only Vaka needs to check (single cell warp)
            aoe.Class = AoEClass.StatueOfGoddess;
            aoe.SkillSource = SkillSource();

            npc.AreaOfEffect = aoe;
            npc.Character.Map!.CreateAreaOfEffect(aoe);
            npc.ExpireEventIfOwnerLeavesMap = true;
        }

        public override bool CanBeAttacked(Npc npc, BattleNpc battleNpc, CombatEntity attacker, CharacterSkill skill = CharacterSkill.None)
        {
            if (!Attackable(npc))
                return false;
            if (skill == CharacterSkill.None) return AllowAutoAttackMove;

            var attr = SkillHandler.GetSkillAttributes(skill);
            return attr.SkillTarget == SkillTarget.Ground && attr.SkillClassification == SkillClass.Physical;
        }
        public override void OnCalculateDamage(Npc npc, BattleNpc battleNpc, CombatEntity attacker, ref DamageInfo di)
        {
            // Let the unfinished statue be moved around by skills
            if (di.AttackSkill != CharacterSkill.None && AllowAutoAttackMove)
                di.KnockBack = 1;

            if (npc.EffectType == NpcEffectType.StatueOfGoddessWoodBlock)
            {
                npc.ValuesInt[5]++;

                // Allow 3 hits
                if (npc.ValuesInt[5] >= 3)
                {
                    // Stop player's auto-attack motion
                    attacker.Player.AutoAttackLock = false;
                }
            }
        }

        public override void OnApplyDamage(Npc npc, BattleNpc battleNpc, ref DamageInfo di)
        {
            di.Damage = 0;

            if (di.KnockBack > 0 && npc.AreaOfEffect != null)
            {
                npc.Character.Map?.MoveAreaOfEffect(npc.AreaOfEffect, Area.CreateAroundPoint(npc.Character.Position, 1));
            }

            if (npc.EffectType == NpcEffectType.StatueOfGoddessWoodBlock)
            {
                if (npc.ValuesInt[5] >= 3)
                {
                    // Make finished statue untargetable by AoEs
                    npc.Character.SetSpawnImmunity(9999f);

                    // Finished statue effect begins
                    switch (SkillSource())
                    {
                        case CharacterSkill.StatueOfGoddessVakarine:
                            npc.ChangeEffectType(NpcEffectType.StatueOfGoddessVakarine);
                            npc.StartTimer(1000);
                            break;
                        case CharacterSkill.StatueOfGoddessCarveOwl:
                            npc.ChangeEffectType(NpcEffectType.StatueOfGoddessCarveOwl);
                            npc.StartTimer(2000);
                            break;
                    }
                }
            }
        }

        public override void OnTimer(Npc npc, float lastTime, float newTime)
        {
            bool eventEnded = false;
            if (newTime > Duration(npc.ValuesInt[0]))
            {
                OnNaturalExpiration(npc);
                npc.StopTimer();
                npc.ResetTimer();
                npc.EndEvent();
                eventEnded = true;
            }

            switch (SkillSource())
            {
                case CharacterSkill.StatueOfGoddessVakarine:
                    // End Vakarine after 10 activations
                    if (npc.ValuesInt[3] >= 10)
                    {
                        npc.EndEvent();
                        eventEnded = true;
                    }

                    if (eventEnded)
                    {
                        if (npc.Owner.TryGet<WorldObject>(out var player))
                            player.Player.SpawnedAoes!.Remove(new KeyValuePair<CharacterSkill, Position>(CharacterSkill.StatueOfGoddessVakarine, npc.SelfPosition));
                    }
                    break;
                case CharacterSkill.StatueOfGoddessCarveOwl:
                    // Trigger Owl every 2 seconds (timer 2000)
                    if (npc.Owner.TryGet<CombatEntity>(out var owner))
                    {
                        TriggerStatue(npc, owner, null, npc.ValuesInt[0]);

                        // Increment the skill rotation param loop (1~4) used by Owl
                        npc.ValuesInt[1] = npc.ValuesInt[1] == 1 ? 2 : 1;
                        //npc.ValuesInt[1] = npc.ValuesInt[1] switch // ToDo: uncomment
                        //{
                        //    1 => 2,
                        //    2 => 3,
                        //    3 => 4,
                        //    4 => 1,
                        //    _ => 1
                        //};
                    }
                    break;
            }
        }

        public virtual void OnNaturalExpiration(Npc npc) { }

        public abstract bool TriggerStatue(Npc npc, CombatEntity src, CombatEntity? target, int skillLevel);

        public override void OnAoEInteraction(Npc npc, CombatEntity target, AreaOfEffect aoe)
        {
            switch (SkillSource())
            {
                case CharacterSkill.StatueOfGoddessVakarine:

                    if (!npc.Owner.TryGet<CombatEntity>(out var owner) || owner.Character.Map != npc.Character.Map)
                    {
                        npc.ValuesInt[3] = 10;
                    }
                    else
                    {
                        TriggerStatue(npc, owner, target, npc.ValuesInt[0]);
                    }
                    break;
                case CharacterSkill.StatueOfGoddessCarveOwl:
                    // Nothing
                    break;
            }
        }
    }
}
