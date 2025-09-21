using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;
using RoRebuildServer.EntityComponents;
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

            if (!isIndirect) 
            {
                // Validate overlapping AoEs ONLY if the skill was manually cast
                var distance = 0;

                var effectiveArea = Area.CreateAroundPoint(position, distance);
                if (map.DoesAreaOverlapWithTrapsOrCharacters(effectiveArea))
                {
                    if (source.Character.Type == CharacterType.Player)
                        CommandBuilder.SkillFailed(source.Player, SkillValidationResult.TargetAreaOccupied);
                    return false;
                }
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

            EntitySystem.Entity e;
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

            e = World.Instance.CreateEvent(source.Entity, map, GroundUnitType(), position, param1, param2, param3, param4, paramString); 

            Npc eventNpc = e.Get<Npc>();
            eventNpc.SummonMobsNearby(1, "GREEN_PLANT", 0, 0);

            // ToDo: encapsulate "wood block monster" code inside npc summon method
            if (eventNpc.Mobs![0].TryGet<RoRebuildServer.EntityComponents.Monster>(out RoRebuildServer.EntityComponents.Monster mon))
            {
                // Removes EXP and Drops from monster (wood block placeholder)
                mon.GivesExperience = false;
                mon.Character.Name = "Wood"; // ToDo: Doesn't work
                mon.SetStat(CharacterStat.Luk, 99);

                //if(SkillType() == CharacterSkill.StatueOfGoddessCarveOwl) // ToDo: Send emote on skill start, after statue is done
                //    mon.ForceSendEmote(29); // /gg
            }
            
            if (eventNpc.Mobs![0].TryGet<CombatEntity>(out var ce))
            {
                // Adds a special status effect to ensure client knows what to draw over the monster sprite
                ce.AddStatusEffect(StatusEffectState.NewStatusEffect(CharacterStatusEffect.WoodBlock1, int.MaxValue));
                ce.SetStat(CharacterStat.MaxHp, 3);
                ce.SetStat(CharacterStat.Hp, 3);
            }

            //eventNpc.ValuesInt[0] = source.Character.Id; // ToDo: OwnerId for other statues? (buff type)
            //eventNpc.ValuesString[1] = SkillType().ToString();

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

        public override void OnMobKill(Npc npc)
        {
            switch (SkillSource())
            {
                case CharacterSkill.StatueOfGoddessVakarine:
                    //npc.AreaSkillIndirect(npc.Character.Position, CharacterSkill.StatueOfGoddessVakarine, 1); // ToDo: Test other statues
                    npc.ChangeEffectType(NpcEffectType.StatueOfGoddessVakarine);
                    npc.StartTimer(1000);
                    break;
                case CharacterSkill.StatueOfGoddessCarveOwl:
                    npc.ChangeEffectType(NpcEffectType.StatueOfGoddessCarveOwl);
                    npc.StartTimer(2000);
                    break;
            }
        }

        public override void InitEvent(Npc npc, int param1, int param2, int param3, int param4, string? paramString)
        {
            if (paramString == null)
                ServerLogger.LogErrorWithStackTrace($"Attempting to create StatueOfGoddessBaseEvent without a string parameter!");

            npc.RevealAsEffect(NpcEffectType.StatueOfGoddessWoodBlock, "StatueOfGoddessWoodBlock");
            npc.ValuesInt[0] = param1; // skill level
            npc.ValuesInt[1] = param2; // xPos for Vaka, skillRotationIndex for Owl
            npc.ValuesInt[2] = param3; // yPos for Vaka
            npc.ValuesInt[3] = param4; // usage counter for Vaka
            npc.ValuesString[0] = paramString!; // mapName for Vaka

            if (!npc.Owner.TryGet<WorldObject>(out var owner))
            {
                ServerLogger.LogWarning($"Npc {npc.Character} running StatueOfGoddess init but does not have an owner.");
                return;
            }

            int distance = 0;
            float tickRate = 1f;
            TargetingInfo targeting = new TargetingInfo()
            {
                Faction = owner.Type == CharacterType.Player ? 1 : 0,
                Party = 0,
                IsPvp = false,
                SourceEntity = npc.Owner,
                TargetingType = TargetingType.Player 
            };

            switch (SkillSource())
            {
                case CharacterSkill.StatueOfGoddessVakarine:
                    targeting.TargetingType = TargetingType.Player; // ToDo: test using Party
                    tickRate = 1f;
                    break;
                case CharacterSkill.StatueOfGoddessCarveOwl:
                    targeting.TargetingType = TargetingType.Enemies;
                    tickRate = 0.25f;
                    distance = 3; // ToDo: remove since ground AoE isn't used for damage anymore
                    npc.ValuesInt[1] = 1; // Initialize with the first skill to be cast by Owl
                    break;
            }

            var aoe = World.Instance.GetNewAreaOfEffect();
            aoe.Init(npc.Character, Area.CreateAroundPoint(npc.Character.Position, distance), AoeType.SpecialEffect, targeting, Duration(npc.ValuesInt[0]), tickRate, 0, 0);
            aoe.TriggerOnFirstTouch = true; // ToDo: review
            aoe.CheckStayTouching = true; // ToDo: review
            aoe.Class = AoEClass.StatueOfGoddess;
            aoe.SkillSource = SkillSource();

            npc.AreaOfEffect = aoe;
            npc.Character.Map!.CreateAreaOfEffect(aoe);
            npc.ExpireEventIfOwnerLeavesMap = true;
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
                    if (npc.Owner.TryGet<CombatEntity>(out var owner))
                    {
                        TriggerStatue(npc, owner, null, npc.ValuesInt[0]);

                        // Increment the skill rotation param loop (1~4) used by Owl
                        npc.ValuesInt[1] = npc.ValuesInt[1] switch
                        {
                            1 => 2,
                            2 => 3,
                            3 => 4,
                            4 => 1,
                            _ => 1
                        };
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
