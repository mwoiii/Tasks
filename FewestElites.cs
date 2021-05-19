using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;

namespace Tasks
{
    class FewestElites : Task
    {
        public override TaskType type { get; } = TaskType.FewestElites;

        int[] kills;
        bool active = false;

        public override bool CanActivate(int numPlayers)
        {
            return numPlayers > 1;
        }

        public override string GetDescription()
        {
            return "Kill the fewest elites.";
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set hooks in FewestElites. {numPlayers} players");
            base.SetHooks(numPlayers);

            GlobalEventManager.onCharacterDeathGlobal += OnKill;

            kills = new int[numPlayers];
            Reset();

            active = true;
        }

        protected override void Unhook()
        {
            if (!active)
                return;
            active = false;

            Evaluate();

            GlobalEventManager.onCharacterDeathGlobal -= OnKill;

            Reset();

            base.Unhook();
        }

        public void OnKill(DamageReport damageReport)
        {
            if (damageReport is null) return;
            if (damageReport.attackerMaster is null) return;
            if (damageReport.attackerMaster.playerCharacterMasterController is null) return;

            int playerNum = TasksPlugin.GetPlayerNumber(damageReport.attackerMaster);

            if(damageReport.victimIsElite)
            {
                kills[playerNum]++;
            }

            UpdateProgress();
        }

        void UpdateProgress()
        {
            int mostKills = 0;
            for (int i = 0; i < kills.Length; i++)
            {
                int count = kills[i];
                if (count > mostKills)
                {
                    mostKills = count;
                }
            }
            if (mostKills > 0)
            {
                for (int i = 0; i < progress.Length; i++)
                {
                    progress[i] = ((float)kills[i] / (float)mostKills);
                }
            }
            else
            {
                for (int i = 0; i < progress.Length; i++)
                {
                    progress[i] = 0;
                }
            }

            base.UpdateProgress(progress);
        }

        void Evaluate()
        {
            if (kills is null)
                return;
            int bestPlayer = 0;
            int leastKills = kills[0];
            // player 0 is base case; skip them
            for (int i = 1; i < kills.Length; i++)
            {
                int count = kills[i];
                if (count < leastKills)
                {
                    bestPlayer = i;
                    leastKills = count;
                }
            }

            Debug.Log($"Player {bestPlayer} won with {leastKills} elite kills");
            CompleteTask(bestPlayer);
        }

        void Reset()
        {
            if (kills is null)
                return;

            for (int i = 0; i < kills.Length; i++)
            {
                kills[i] = 0;
            }
            ResetProgress();
        }
    }
}
