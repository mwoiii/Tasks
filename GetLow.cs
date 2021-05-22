using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;

namespace Tasks
{
    class GetLow : Task
    {
        public override TaskType type { get; } = TaskType.GetLow;

        float[] lowPercents;
        bool active = false;

        public override bool CanActivate(int numPlayers)
        {
            return true; // numPlayers > 1;
        }

        public override string GetDescription()
        {
            return "Get to the lowest life. No dying.";
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set Hooks in GetLow. {numPlayers} players");

            base.SetHooks(numPlayers);

            GlobalEventManager.onServerDamageDealt += OnDamage;

            lowPercents = new float[numPlayers];
            Reset();
        }

        protected override void Unhook()
        {
            if (!active)
                return;
            active = false;

            Evaluate();

            GlobalEventManager.onServerDamageDealt -= OnDamage;

            Reset();

            base.Unhook();
        }

        void OnDamage(DamageReport report)
        {
            if (report is null) return;
            if (report.victimMaster.playerCharacterMasterController is null) return;

            int playerNum = TasksPlugin.GetPlayerNumber(report.victimMaster);

            // should work with transcendence (1hp + shields)
            // works with REX
            // does this trigger on fall damage? Should. report.isFallDamage exists

            // fullCombinedHealth is maxHp + maxShield(blue). Doesn't count gold/orange barrier
            float hpPercent = 100 * (report.victim.health + report.victim.shield) / report.victim.fullCombinedHealth; 
            //Debug.Log($"Player {playerNum} hit. {report.victim.health}/{report.victim.fullHealth} ({hpPercent}%) Shield: {report.victim.shield}/{report.victim.fullShield} {report.victim.fullCombinedHealth}");
            // 0% is dead. Need to not die to count
            if(hpPercent > 0)
            {
                if(hpPercent < lowPercents[playerNum])
                {
                    lowPercents[playerNum] = hpPercent;
                    UpdateProgress();
                }
            }
            else
            {
                // your died. You are DQed
                lowPercents[playerNum] = 101;
                UpdateProgress();
            }
        }

        protected void UpdateProgress()
        {
            for (int i = 0; i < progress.Length; i++)
            {
                progress[i] = (100 - lowPercents[i]) / 100f;
            }
            base.UpdateProgress(progress);
        }

        void Evaluate()
        {
            float lowest = 100;
            int winner = 0;

            for (int i = 0; i < lowPercents.Length; i++)
            {
                if(lowPercents[i] < lowest)
                {
                    lowest = lowPercents[i];
                    winner = i;
                }
            }
            CompleteTask(winner);
        }

        void Reset()
        {
            if (lowPercents is null)
                return;

            for (int i = 0; i < lowPercents.Length; i++)
            {
                lowPercents[i] = 100;
            }
            ResetProgress();
        }

    }
}
