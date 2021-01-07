using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tasks
{
    class AirKills : Task
    {

        // so doing just this doesn't seem to work
        //protected static new string thisClass = "AIR_KILLS_";
        //  /Logbook/LOGBOOK_CATEGORY_ACHIEVEMENTS/SOLRUN_TEMP_ACHIEVEMENTS_KILL_BEETLE_ACHIEVEMENT_NAME /Logbook/LOGBOOK_CATEGORY_ACHIEVEMENTS/SOLRUN_TASKS_BASE_TASK_ACHIEVEMENT_NAME

        public override string AchievementIdentifier { get; } = "SOLRUN_TASKS_AIRKILLS_ACHIEVEMENT_ID";
        public override string UnlockableIdentifier { get; } = "";
        // I think all this does is hide it in the log until you have the prereq. You could still complete it (except most prereqs seem to be characters)
        public override string PrerequisiteUnlockableIdentifier { get; } = "";
        public override string AchievementNameToken { get; } = "Air Kills"; // plain English
        public override string AchievementDescToken { get; } = "Get 3 kills whilst airborne"; // plain English
        public override string UnlockableNameToken { get; } = ""; // plain English

        protected override int _id { get; } = 1;

        int kills = 0;
        int killsNeeded = 3;

        protected override void SetHooks()
        {
            kills = 0;

            base.SetHooks();
            GlobalEventManager.onCharacterDeathGlobal += OnKill;
            
            ownerCached.localUser.cachedBody.characterMotor.onHitGround += OnLanding;
            //Chat.AddMessage("Set hooks for AirKills");
        }

        protected override void Unhook()
        {
            GlobalEventManager.onCharacterDeathGlobal -= OnKill;
            ownerCached.localUser.cachedBody.characterMotor.onHitGround -= OnLanding;

            base.Unhook();
        }

        protected override bool IsComplete()
        {
            return kills >= killsNeeded;
        }

        public void OnKill(DamageReport damageReport)
        {
            if (damageReport is null) return;
            if (damageReport.attackerMaster is null) return;

            //if (damageReport.victimMaster is null) return;
            //Chat.AddMessage(String.Format("Killer: {0} Me: {1} Victim: {2}", damageReport.attackerMaster.ToString(), damageReport.victimMaster.ToString(), ownerCached.localUser.cachedMaster.ToString()));
            //[Info: Unity Log] Killer: CommandoMaster(Clone)(RoR2.CharacterMaster) Me: LemurianMaster(Clone)(RoR2.CharacterMaster) Victim: CommandoMaster(Clone)(RoR2.CharacterMaster)
            // Did I kill it?
            if (damageReport.attackerMaster == ownerCached.localUser.cachedMaster)
            {
                if (Airborne())
                {
                    kills++;
                    if (IsComplete())
                    {
                        EndTask();
                    }
                }
            }
        }

        bool Airborne()
        {
            // characterMotor.lastGroundTime
            //CharacterMotor m;
            //m.isGrounded
            return !ownerCached.localUser.cachedBody.characterMotor.isGrounded;
        }

        public void OnLanding(ref CharacterMotor.HitGroundInfo hitGroundInfo)
        {
            kills = 0;
        }
    }
}
