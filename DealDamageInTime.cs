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

        protected new string description { get; } = "Deal 500 damage in 5 seconds";
        public override TaskType type { get; } = TaskType.DamageInTime;
        protected override string name { get; } = "Damage In Time";

        float damageToDeal = 500;
        float timeLimit = 5;

        float[] currentDamage;

        // used for the progress bar. So the bar doesn't empty after the task is over
        // I want to keep the bar full for a few sec
        bool active = false;    

        public override string GetDescription()
        {
            // N0 turns it into a number with 0 decimal places i.e 5672.35 -> 5,672
            return $"Deal {damageToDeal:N0} damage in 5 seconds";
        }

        public override string GetWinMessage(int winningPlayer)
        {
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by dealing {GetStylizedTaskWinStat(damageToDeal.ToString())} damage within 5s.";
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set Hooks in DamageInTime. {numPlayers} players");

            base.SetHooks(numPlayers);

            GlobalEventManager.onServerDamageDealt += OnDamage;
            damageToDeal = Run.instance.difficultyCoefficient * damageToDeal;

            if (currentDamage is null || currentDamage.Length != numPlayers)
            {
                currentDamage = new float[numPlayers];
            }
            active = true;
            Reset();
        }

        protected override void Unhook()
        {
            GlobalEventManager.onServerDamageDealt -= OnDamage;

            Reset();

            base.Unhook();
        }

        void UpdateProgress()
        {
            for (int i = 0; i < progress.Length; i++)
            {
                progress[i] = currentDamage[i] / damageToDeal;
            }
            base.UpdateProgress(progress);
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
            UpdateProgress();

            // This class isn't a monobehaviour so it can't start its own coroutines
            // So this is a workaround. 
            TasksPlugin.instance.StartCoroutine(ReduceDamage(report.damageDealt, playerNum));
            if (IsComplete(playerNum))
            {
                //Chat.AddMessage($"Player {playerNum} Completed DamageInTime");

                active = false;
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
            if(active)
                UpdateProgress();
        }

        void Reset()
        {
            if (currentDamage is null)
                return;
            for (int i = 0; i < currentDamage.Length; i++)
            {
                currentDamage[i] = 0;
            }
            ResetProgress();
        }
    }
}
