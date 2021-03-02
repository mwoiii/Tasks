using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tasks
{
    class DamageMultipleTargets : Task
    {
        protected new string description { get; } = "Have 4 enemies damaged by you alive at once";
        /*
        public override string AchievementIdentifier { get; } = "SOLRUN_TASKS_DAMAGE_MULTIPLE_ACHIEVEMENT_ID"; // delete this from XML if there
        public override string UnlockableIdentifier { get; } = "SOLRUN_TASKS_DAMAGE_MULTIPLE_REWARD_ID"; // Delete me from XML too
        // I think all this does is hide it in the log until you have the prereq. You could still complete it (except most prereqs seem to be characters)
        public override string PrerequisiteUnlockableIdentifier { get; } = "";
        public override string AchievementNameToken { get; } = "SOLRUN_TASKS_DAMAGE_MULTIPLE_ACHIEVEMENT_NAME"; // Fine to have in the XML
        public override string AchievementDescToken { get; } = description; // plain English
        public override string UnlockableNameToken { get; } = ""; // plain English
        */
        public override TaskType type { get; } = TaskType.DamageMultiple;
        protected override string name { get; } = "Damage Multiple";

        int numToHit = 4;
        // GameObject seems the way to go. Pretty sure they are unique
        // using body might not work because multiple things have the same body 
        // so maybe I can't tell them apart? Like maybe I can't add 2 different beetles to the hash
        HashSet<GameObject>[] targets;

        public override string GetDescription()
        {
            return description;
        }

        protected override void SetHooks(int numPlayers)
        {
            Chat.AddMessage($"Set Hooks in DamageMultiple. {numPlayers} players");

            base.SetHooks(numPlayers);


            GlobalEventManager.onServerDamageDealt += OnDamage;
            GlobalEventManager.onCharacterDeathGlobal += OnKill;

            targets = new HashSet<GameObject>[numPlayers];
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i] = new HashSet<GameObject>();
            }
        }

        protected override void Unhook()
        {
            GlobalEventManager.onServerDamageDealt -= OnDamage;
            GlobalEventManager.onCharacterDeathGlobal -= OnKill;

            // for when the task ends without someone completing it
            ResetKills();

            base.Unhook();
        }

        public void OnDamage(DamageReport report)
        {
            if (report is null) return;
            if (report.attackerMaster is null) return;
            if (report.attackerMaster.playerCharacterMasterController is null) return;

            // Did I kill it?
            int playerNum = TasksPlugin.GetPlayerNumber(report.attackerMaster);

            if (report.victim.alive)
            {
                if (targets[playerNum].Contains(report.victim.gameObject))
                    return;
                targets[playerNum].Add(report.victim.gameObject);
                if(IsComplete(playerNum))
                {
                    Chat.AddMessage($"Player {playerNum} Completed DamageMultiple");
                    CompleteTask(playerNum);
                    // reset
                    ResetKills();
                }
            }
        }

        protected override bool IsComplete(int playerNum)
        {
            return targets[playerNum].Count >= numToHit;
        }

        void OnKill(DamageReport report)
        {
            if (report is null) return;
            if (report.attackerMaster is null) return;
            if (report.attackerMaster.playerCharacterMasterController is null) return;

            // Did I kill it?
            int playerNum = TasksPlugin.GetPlayerNumber(report.attackerMaster);

            targets[playerNum].Remove(report.victim.gameObject);
        }

        void ResetKills()
        {
            if (targets is null)
                return;
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].Clear();
            }
        }
    }
}
