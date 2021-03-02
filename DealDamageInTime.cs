using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;

namespace Tasks
{
    class DealDamageInTime : Task
    {

        public static new string description { get; } = "Deal 500 damage in 5 seconds";
        /*
        public override string AchievementIdentifier { get; } = "SOLRUN_TASKS_DAMAGE_IN_TIME_ACHIEVEMENT_ID"; // delete this from XML if there
        public override string UnlockableIdentifier { get; } = "SOLRUN_TASKS_DAMAGE_IN_TIME_REWARD_ID"; // Delete me from XML too
        // I think all this does is hide it in the log until you have the prereq. You could still complete it (except most prereqs seem to be characters)
        public override string PrerequisiteUnlockableIdentifier { get; } = "";
        public override string AchievementNameToken { get; } = "SOLRUN_TASKS_DAMAGE_IN_TIME_ACHIEVEMENT_NAME"; // Fine to have in the XML
        public override string AchievementDescToken { get; } = description; // plain English
        public override string UnlockableNameToken { get; } = ""; // plain English
        */
        public override TaskType type { get; } = TaskType.DamageInTime;
        protected override string name { get; } = "Damage In Time";

        float damageToDeal = 500;
        float timeLimit = 5;

        float[] currentDamage;

        protected override void SetHooks(int numPlayers)
        {
            Chat.AddMessage($"Set Hooks in DamageInTime. {numPlayers} players");

            base.SetHooks(numPlayers);

            GlobalEventManager.onServerDamageDealt += OnDamage;
            damageToDeal = Run.instance.difficultyCoefficient * damageToDeal;

            if (currentDamage is null || currentDamage.Length != numPlayers)
            {
                currentDamage = new float[numPlayers];
            }
            Reset();
        }

        protected override void Unhook()
        {
            GlobalEventManager.onServerDamageDealt -= OnDamage;

            Reset();

            base.Unhook();
        }

        public override string GetDescription()
        {
            return $"Deal {damageToDeal} damage in 5 seconds";
        }

        public void OnDamage(DamageReport report)
        {
            if (report is null) return;
            if (report.attackerMaster is null) return;
            if (report.attackerMaster.playerCharacterMasterController is null) return;

            // Did I hit it?
            int playerNum = TasksPlugin.GetPlayerNumber(report.attackerMaster);

            // Does this count fall damage?
            // report.isFallDamage exists so probably
            // I guess it doesn't matter if you use fall damage to complete this
            // I wonder if it counts overkill?
            currentDamage[playerNum] += report.damageDealt;

            // This class isn't a monobehaviour so it can't start its own coroutines
            // So this is a workaround. 
            TasksPlugin.instance.StartCoroutine(ReduceDamage(report.damageDealt, playerNum));
            if (IsComplete(playerNum))
            {
                Chat.AddMessage($"Player {playerNum} Completed DamageInTime");

                CompleteTask(playerNum);
                Reset();
            }
        }

        protected override bool IsComplete(int playerNum)
        {
            return currentDamage[playerNum] >= damageToDeal;
        }

        IEnumerator ReduceDamage(float damage, int playerNum)
        {
            yield return new WaitForSeconds(timeLimit);
            currentDamage[playerNum] = Mathf.Max(0, currentDamage[playerNum] - damage);
        }

        void Reset()
        {
            if (currentDamage is null)
                return;
            for (int i = 0; i < currentDamage.Length; i++)
            {
                currentDamage[i] = 0;
            }
        }
    }
}
