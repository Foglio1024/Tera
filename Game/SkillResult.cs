﻿using System;
using Tera.Game.Messages;

namespace Tera.Game
{
    public class SkillResult
    {
        public SkillResult(EachSkillResultServerMessage message, EntityTracker entityRegistry,
            PlayerTracker playerTracker, SkillDatabase skillDatabase, PetSkillDatabase petSkillDatabase = null)
        {
            Time = message.Time;
            Amount = message.Amount;
            IsCritical = message.IsCritical;
            IsHp = message.IsHp;
            IsHeal = message.IsHeal;
            SkillId = message.SkillId;
            Abnormality = false;

            Source = entityRegistry.GetOrPlaceholder(message.Source);
            Target = entityRegistry.GetOrPlaceholder(message.Target);
            var userNpc = UserEntity.ForEntity(Source);
            var npc = (NpcEntity) userNpc["npc"];
            var sourceUser = userNpc["user"] as UserEntity; // Attribute damage dealt by owned entities to the owner
            var targetUser = Target as UserEntity; // But don't attribute damage received by owned entities to the owner

            if (sourceUser != null)
            {
                Skill = skillDatabase.Get(sourceUser, message);
                if (Skill == null && npc != null)
                {
                    Skill = new UserSkill(message.SkillId, sourceUser.RaceGenderClass, npc.Info.Name, null,
                        petSkillDatabase?.Get(npc.Info.Name, SkillId) ?? "",
                        skillDatabase.GetSkillByPetName(npc.Info.Name, sourceUser.RaceGenderClass)?.IconName ?? "",
                        npc.Info);
                }
                SourcePlayer = playerTracker.Get(sourceUser.ServerId, sourceUser.PlayerId);
                if (Skill == null)
                    Skill = new UserSkill(message.SkillId, sourceUser.RaceGenderClass, "Unknown");
            }
            if (targetUser != null)
            {
                TargetPlayer = playerTracker.Get(targetUser.ServerId, targetUser.PlayerId);
            }

            Source.Position = Source.Position.MoveForvard(Source.Finish, Source.Speed,
                message.Time.Ticks - Source.StartTime);
            if (Source.EndTime > 0 && Source.EndTime <= Source.StartTime)
            {
                Source.Heading = Source.EndAngle;
                Source.EndTime = 0;
            }
            Target.Position = Target.Position.MoveForvard(Target.Finish, Target.Speed,
                message.Time.Ticks - Target.StartTime);
            if (Target.EndTime > 0 && Target.EndTime <= Target.StartTime)
            {
                Target.Heading = Target.EndAngle;
                Target.EndTime = 0;
            }
            HitDirection = Source.Position.GetHeading(Target.Position).HitDirection(Target.Heading);
            //Debug.WriteLine($"{Source} {Source.Position} {Target} {Target.Position} {Target.Heading} {HitDirection}");
        }

        public SkillResult(int amount, bool isCritical, bool isHp, bool isHeal, HotDot hotdot, EntityId source,
            EntityId target, DateTime time,
            EntityTracker entityRegistry, PlayerTracker playerTracker)
        {
            Time = time;
            Amount = amount;
            IsCritical = isCritical;
            IsHp = isHp;
            IsHeal = isHeal;
            SkillId = hotdot.Id;
            Abnormality = true;

            Source = entityRegistry.GetOrPlaceholder(source);
            Target = entityRegistry.GetOrPlaceholder(target);
            var userNpc = UserEntity.ForEntity(Source);
            var sourceUser = userNpc["user"] as UserEntity; // Attribute damage dealt by owned entities to the owner
            var targetUser = Target as UserEntity; // But don't attribute damage received by owned entities to the owner

            var pclass = PlayerClass.Common;
            if (sourceUser != null)
            {
                SourcePlayer = playerTracker.Get(sourceUser.ServerId, sourceUser.PlayerId);
                pclass = SourcePlayer.RaceGenderClass.Class;
            }
            Skill = new UserSkill(hotdot.Id, pclass,
                hotdot.Name, "DOT", null, hotdot.IconName);

            if (targetUser != null)
            {
                TargetPlayer = playerTracker.Get(targetUser.ServerId, targetUser.PlayerId);
            }
            HitDirection = HitDirection.Dot;
        }

        public HitDirection HitDirection { get; private set; }
        public DateTime Time { get; private set; }
        public bool Abnormality { get; }
        public int Amount { get; }
        public Entity Source { get; }
        public Entity Target { get; }
        public bool IsCritical { get; private set; }
        public bool IsHp { get; }
        public bool IsHeal { get; }

        public int SkillId { get; }
        public Skill Skill { get; }
        public string SkillName => Skill?.Name ?? SkillId.ToString();
        public string SkillShortName => Skill?.ShortName ?? SkillId.ToString();

        public string SkillNameDetailed
            =>
                $"{Skill?.Name ?? SkillId.ToString()} {(IsChained != null ? (bool) IsChained ? "[C]" : null : null)} {(string.IsNullOrEmpty(Skill?.Detail) ? null : $"({Skill.Detail})")}"
                    .Replace("  ", " ");

        public bool? IsChained => Skill?.IsChained;

        public int Damage => IsHeal || !IsHp ? 0 : Amount;

        public int Heal => IsHp && IsHeal ? Amount : 0;
        public int Mana => !IsHp ? Amount : 0;


        public Player SourcePlayer { get; }
        public Player TargetPlayer { get; private set; }


        public static Skill GetSkill(EntityId sourceid, int skillid, bool hotdot, EntityTracker entityRegistry,
            SkillDatabase skillDatabase, HotDotDatabase hotdotDatabase, PetSkillDatabase petSkillDatabase = null)
        {
            if (hotdot)
            {
                var hotdotskill = hotdotDatabase.Get(skillid);
                return new Skill(skillid, hotdotskill.Name, null, "", hotdotskill.IconName, null, true);
            }

            var source = entityRegistry.GetOrPlaceholder(sourceid);
            var userNpc = UserEntity.ForEntity(source);
            var npc = (NpcEntity) userNpc["npc"];
            var sourceUser = userNpc["user"] as UserEntity; // Attribute damage dealt by owned entities to the owner

            Skill skill = null;
            if (sourceUser != null)
            {
                skill = skillDatabase.GetOrNull(sourceUser.RaceGenderClass, skillid);
                if (skill == null && npc != null)
                {
                    skill = new UserSkill(skillid, sourceUser.RaceGenderClass, npc.Info.Name, null,
                        petSkillDatabase?.Get(npc.Info.Name, skillid) ?? "",
                        skillDatabase.GetSkillByPetName(npc.Info.Name, sourceUser.RaceGenderClass)?.IconName ?? "",
                        npc.Info);
                }
                if (skill == null)
                {
                    skill = new UserSkill(skillid, sourceUser.RaceGenderClass, "Unknown");
                }
            }
            return skill;
        }

        public bool IsValid(DateTime? firstAttack = null)
        {
            return (firstAttack != null || (!IsHeal && Amount > 0)) &&
                   //only record first hit is it's a damage hit (heals occurring outside of fights)
                   !(Target.Equals(Source) && !IsHeal && Amount > 0);
            //disregard damage dealt to self (gunner self destruct)
        }

        public override string ToString()
        {
            return $"{SkillName}({SkillId}) [{Amount}]";
        }
    }
}