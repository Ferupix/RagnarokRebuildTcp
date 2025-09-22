using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;
using RoRebuildServer.Data;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.EntityComponents.Npcs;
using RoRebuildServer.EntityComponents.Util;
using RoRebuildServer.Logging;
using RoRebuildServer.Networking;
using System.Diagnostics;

namespace RoRebuildServer.Simulation.Skills.SkillHandlers.Acolyte
{
    [MonsterSkillHandler(CharacterSkill.HealCleric, SkillClass.Magic, SkillTarget.Ground)]
    [SkillHandler(CharacterSkill.HealCleric, SkillClass.Magic, SkillTarget.Ground)]
    public class HealClericHandler : SkillHandlerBase
    {
        public override SkillValidationResult ValidateTarget(CombatEntity source, CombatEntity? target, Position position,
        int lvl, bool isIndirect, bool isItemSource)
        {
            if (!isIndirect && !CheckRequiredGemstone(source, BlueGemstone, false))
                return SkillValidationResult.MissingRequiredItem;

            return base.ValidateTarget(source, target, position, lvl, false, false);
        }

        public override void Process(CombatEntity source, CombatEntity? target, Position position, int lvl, bool isIndirect, bool isItemSource)
        {
            if (source.Character.Map == null)
                return;

            if (target != null)
                position = target.Character.Position; //monsters and indirect casts will target self, so use that position
            if (position == Position.Invalid)
                position = source.Character.Position; //or self if there's no target (should always have a target though...)

            var ch = source.Character;
            var map = ch.Map;

            var e = World.Instance.CreateEvent(source.Entity, map, "HealClericBaseEvent", position, lvl, 0, 0, 0, null);
            ch.AttachEvent(e);

            if (!isIndirect)
            {
                source.ApplyCooldownForSupportSkillAction();
                CommandBuilder.SkillExecuteAreaTargetedSkillAutoVis(ch, position, CharacterSkill.HealCleric, lvl);
            }
        }
    }

    public class HealClericBaseEvent : NpcBehaviorBase
    {
        public override void InitEvent(Npc npc, int param1, int param2, int param3, int param4, string? paramString)
        {
            npc.ValuesInt[0] = param1; //level
            npc.ValuesInt[1] = 1 + 3 * param1; //duration
            npc.ValuesInt[2] = 1; //max activations
            npc.StartTimer(200);

            if (!npc.Owner.TryGet<WorldObject>(out var owner))
            {
                ServerLogger.LogWarning($"Npc {npc.Character} running HealClericBaseEvent init but does not have an owner.");
                return;
            }

            var angle = owner.Position.Angle(npc.SelfPosition);
            var facing = Directions.GetFacingForAngle(angle);

            var position = npc.SelfPosition;
            var map = npc.Character.Map;

            int skillLevel = int.Clamp(param1, 1, 6);

            Span<Position> posList = stackalloc Position[skillLevel];
            var posCount = 0;

            #region CardinalDirectionTilePositioning
            for (int i = 0; i < skillLevel; i++)
            {
                var pos = new Position(position.X, position.Y);

                if (i != 0)
                {
                    switch (facing)
                    {
                        case Direction.North:
                            // 4 3 5
                            // 1 0 2
                            pos += i switch
                            {
                                1 => new Position(-1, 0),
                                2 => new Position(1, 0),
                                3 => new Position(0, 1),
                                4 => new Position(-1, 1),
                                5 => new Position(1, 1),
                                _ => new Position(0, 0)
                            };
                            break;
                        case Direction.South:
                            // 1 0 2
                            // 4 3 5
                            pos += i switch
                            {
                                1 => new Position(-1, 0),
                                2 => new Position(1, 0),
                                3 => new Position(0, -1),
                                4 => new Position(-1, -1),
                                5 => new Position(1, -1),
                                _ => new Position(0, 0)
                            };
                            break;
                        case Direction.East:
                            // 1 4
                            // 0 3
                            // 2 5
                            pos += i switch
                            {
                                1 => new Position(0, 1),
                                2 => new Position(0, -1),
                                3 => new Position(1, 0),
                                4 => new Position(1, 1),
                                5 => new Position(1, -1),
                                _ => new Position(0, 0)
                            };
                            break;
                        case Direction.West:
                            // 4 1
                            // 3 0
                            // 5 2
                            pos += i switch
                            {
                                1 => new Position(0, 1),
                                2 => new Position(0, -1),
                                3 => new Position(-1, 0),
                                4 => new Position(-1, 1),
                                5 => new Position(-1, -1),
                                _ => new Position(0, 0)
                            };
                            break;
                        case Direction.NorthWest:
                            // X X 5 2
                            // X 3 0 X
                            // 4 1 X X
                            pos += i switch
                            {
                                1 => new Position(-1, -1),
                                2 => new Position(1, 1),
                                3 => new Position(-1, 0),
                                4 => new Position(-2, -1),
                                5 => new Position(0, 1),
                                _ => new Position(0, 0)
                            };
                            break;
                        case Direction.SouthEast:
                            // X X 1 4
                            // X 0 3 X
                            // 2 5 X X
                            pos += i switch
                            {
                                1 => new Position(1, 1),
                                2 => new Position(-1, -1),
                                3 => new Position(1, 0),
                                4 => new Position(2, 1),
                                5 => new Position(0, -1),
                                _ => new Position(0, 0)
                            };
                            break;
                        case Direction.SouthWest:
                            // 4 1 X X
                            // X 3 0 X
                            // X X 5 2
                            pos += i switch
                            {
                                1 => new Position(-1, 1),
                                2 => new Position(1, -1),
                                3 => new Position(-1, 0),
                                4 => new Position(-2, 1),
                                5 => new Position(0, -1),
                                _ => new Position(0, 0)
                            };
                            break;
                        case Direction.NorthEast:
                            // 2 5 X X
                            // X 0 3 X
                            // X X 1 4
                            pos += i switch
                            {
                                1 => new Position(1, -1),
                                2 => new Position(-1, 1),
                                3 => new Position(1, 0),
                                4 => new Position(2, -1),
                                5 => new Position(0, 1),
                                _ => new Position(0, 0)
                            };
                            break;
                    }
                }

                if (!map.WalkData.IsCellWalkable(pos))
                    continue;

                posList[posCount++] = pos;
            }

            #endregion CardinalDirectionPositioning

            // X-1T+1, X+0Y+1, X+1T+1
            // X-1Y+0, Target, X+1Y+0
            //for (var x = -1; x <= 1; x++)
            //{
            //    for (var y = 0; y <= 1; y++)
            //    {
            //        var pos = new Position(position.X + x, position.Y + y);

            //        if (!map.WalkData.IsCellWalkable(pos))
            //            continue;

            //        posList[posCount++] = pos;
            //    }
            //}

            for (var i = 0; i < posCount; i++)
            {
                if (npc.Character.Map!.WalkData.IsCellWalkable(posList[i]))
                    npc.CreateEvent("HealClericObjectEvent", posList[i], param1, npc.ValuesInt[2]);
            }
        }

        public override void OnTimer(Npc npc, float lastTime, float newTime)
        {
            Debug.Assert(npc.ValuesInt != null && npc.ValuesInt.Length >= 4);

            npc.Character.Events?.ClearInactive();

            if (npc.EventsCount == 0)
            {
                npc.EndEvent();
                return;
            }

            if (newTime > npc.ValuesInt[1])
            {
                npc.EndAllEvents();
                return;
            }

            if (!npc.Owner.TryGet<CombatEntity>(out var owner)
               || !owner.Character.IsActive
               || owner.Character.Map != npc.Character.Map
               || owner.Character.State == CharacterState.Dead)
                npc.EndAllEvents();
        }

        public override EventOwnerDeathResult OnOwnerDeath(Npc npc, CombatEntity owner)
        {
            if (owner.Character.Type == CharacterType.Monster)
            {
                npc.EndAllEvents();
                return EventOwnerDeathResult.RemoveEvent;
            }

            return EventOwnerDeathResult.NoAction;
        }
    }

    public class HealClericObjectEvent : NpcBehaviorBase
    {
        public override void InitEvent(Npc npc, int param1, int param2, int param3, int param4, string? paramString)
        {
            if (!npc.Owner.TryGet<Npc>(out var parent) || !parent.Owner.TryGet<CombatEntity>(out var source))
            {
                ServerLogger.LogWarning($"Failed to init HealCleric object event as it has no owner or source entity!");
                npc.EndEvent();
                return;
            }

            var targeting = new TargetingInfo()
            {
                Faction = source.Character.Type == CharacterType.Player ? 1 : 0,
                Party = 0,
                IsPvp = false,
                SourceEntity = parent.Owner,
                TargetingType = TargetingType.Everyone
            };

            var aoe = World.Instance.GetNewAreaOfEffect();
            aoe.Init(npc.Character, Area.CreateAroundPoint(npc.Character.Position, 0), AoeType.SpecialEffect, targeting, 1 + 3 * param1, 0.1f, 0, 0);
            aoe.TriggerOnFirstTouch = true;
            aoe.CheckStayTouching = true; //if they die or something in the aoe and lose their status we want to give it back promptly
            aoe.SkillSource = CharacterSkill.HealCleric;

            npc.ValuesInt[0] = param1;
            npc.ValuesInt[1] = param2;
            npc.AreaOfEffect = aoe;
            npc.Character.Map!.CreateAreaOfEffect(aoe);
            npc.StartTimer(200);

            npc.RevealAsEffect(NpcEffectType.HealCleric, "HealCleric");
        }

        public override void OnAoEInteraction(Npc npc, CombatEntity target, AreaOfEffect aoe)
        {
            if (!aoe.TargetingInfo.SourceEntity.TryGet<CombatEntity>(out var src))
                return;

            if (src.Character.Map != npc.Character.Map || !src.CanPerformIndirectActions())
                return;

            if (target != src && !target.IsValidTarget(src, true, true))
                return;

            if (target.IsInSkillDamageCooldown(CharacterSkill.HealCleric))
                return;

            var chLevel = src.GetStat(CharacterStat.Level);
            var statInt = src.GetEffectiveStat(CharacterStat.Int);
            var (min, max) = src.CalculateAttackPowerRange(true);
            var matk = GameRandom.Next(min, max);

            var baseHeal = 4 + 8 * npc.ValuesInt[0] * 2;
            var healValue = (chLevel + statInt) / 10 * baseHeal + matk / 2;

            if (src.Character.Type == CharacterType.Player)
                healValue = healValue * (1000 + src.GetStat(CharacterStat.AddHealingPower)) / 1000;

            if (target.IsElementBaseType(CharacterElement.Undead1) || target.GetRace() == CharacterRace.Demon)
            {
                var res = src.PrepareTargetedSkillResult(target, CharacterSkill.Heal);
                var mod = DataManager.ElementChart.GetAttackModifier(AttackElement.Holy, target.GetElement());
                res.Damage = healValue / 2 * mod / 100;
                res.HitCount = 1;
                res.AttackPosition = target.Character.Position.AddDirectionToPosition(target.Character.FacingDirection);
                res.KnockBack = 0;
                res.AttackMotionTime = 0f;
                res.Time = 0f;
                res.Result = AttackResult.NormalDamage;

                if (src.Character.Type == CharacterType.Player && target.Character.Type == CharacterType.Player && src != target)
                    res.Damage = 1;

                CommandBuilder.SkillExecuteIndirectAutoVisibility(npc.Character, target.Character, res);
                src.ExecuteCombatResult(res, false);
            }
            else
            {
                if (target.GetStat(CharacterStat.Hp) >= target.GetStat(CharacterStat.MaxHp))
                    return;

                target.HealHp(healValue, true, HealType.HealSkill);
            }

            npc.ValuesInt[1]--; // decrement max activations

            target.SetSkillDamageCooldown(CharacterSkill.HealCleric, 0.5f);
        }

        public override void OnTimer(Npc npc, float lastTime, float newTime)
        {
            if (newTime > 15 || npc.ValuesInt[1] <= 0)
                npc.EndEvent();
        }
    }

    public class NpcLoaderHealClericEvents : INpcLoader
    {
        public void Load()
        {
            DataManager.RegisterEvent("HealClericBaseEvent", new HealClericBaseEvent());
            DataManager.RegisterEvent("HealClericObjectEvent", new HealClericObjectEvent());
        }
    }
}
