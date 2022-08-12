using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tasks
{
    class FewElites : Task
    {
        public override TaskType type { get; } = TaskType.FewElites;

        protected override string name { get; } = "Few Elites";

        int[] kills;
        int maxKills = 5;
        bool taskFinished = false;

        public override bool CanActivate(int numPlayers)
        {
            return true;
        }

        public override string GetDescription()
        {
            return "Kill the boss before you kill 5 elites.";
        }

        public override string GetWinMessage(int winningPlayer)
        {
            if(winningPlayer < 0)
            {
                return $"You failed {GetStylizedTaskName(name)} by killing too many elites.";
            }
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by killing the boss while only killing {GetStylizedTaskWinStat(kills[winningPlayer].ToString())}/{maxKills} elites.";
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set hooks in FewElites. {numPlayers} players");
            base.SetHooks(numPlayers);

            GlobalEventManager.onCharacterDeathGlobal += OnKill;
            BossGroup.onBossGroupDefeatedServer += BossDied;

            kills = new int[numPlayers];
            taskFinished = false;
            Reset();
            UpdateProgress();
        }

        void BossDied(BossGroup group)
        {
            // boss is dead
            Evaluate();
            taskFinished = true;
        }

        protected override void StageEnd()
        {
            base.StageEnd();

            Reset();
        }

        protected override void Unhook()
        {
            GlobalEventManager.onCharacterDeathGlobal -= OnKill;
            BossGroup.onBossGroupDefeatedServer -= BossDied;


            base.Unhook();
        }

        public void OnKill(DamageReport damageReport)
        {
            if (taskFinished) return;

            if (damageReport is null) return;
            if (damageReport.attackerMaster is null) return;
            if (damageReport.attackerMaster.playerCharacterMasterController is null) return;

            int playerNum = TasksPlugin.GetPlayerNumber(damageReport.attackerMaster);

            if (damageReport.victimIsElite)
            {
                kills[playerNum]++;
                if(totalNumberPlayers == 1)
                {
                    if(kills[playerNum] > maxKills)
                    {
                        // -1 is the failure state
                        CompleteTask(-1);
                    }
                }
            }

            UpdateProgress();
        }

        void UpdateProgress()
        {
            
            for (int i = 0; i < progress.Length; i++)
            {
                if (taskFinished)
                {
                    progress[i] = 0;
                }
                else
                {
                    // 1 is 0/10
                    // 0.8 is 2/10
                    // 0.5 is 5/10
                    // 0 is 10+/10
                    float prog = (float)(maxKills - kills[i]) / (float)maxKills;
                    // this will go negative at 11 kills
                    prog = Mathf.Max(prog, 0);
                    progress[i] = prog;
                }
            }

            base.UpdateProgress(progress);
        }

        void Evaluate()
        {
            if (kills is null)
                return;

            string winnersDebug = "";
            string killsDebug = "";
            for (int i = 0; i < kills.Length; i++)
            {
                if(kills[i] <= maxKills)
                {
                    CompleteTask(i);
                    winnersDebug += $"{i}, ";
                    killsDebug += $"{kills[i]}, ";
                }
            }

            Debug.Log($"Players {winnersDebug} won with {killsDebug} /{maxKills} kills");
        }

        void Reset()
        {
            if (kills is null)
                return;

            taskFinished = false;
            for (int i = 0; i < kills.Length; i++)
            {
                kills[i] = 0;
            }
            ResetProgress();
        }
    }
}
