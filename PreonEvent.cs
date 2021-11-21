using System;
using System.Collections.Generic;
using System.Text;
using R2API.Utils;
using RoR2;

namespace Tasks
{
    class PreonEvent : Task
    {
        protected new string description { get; } = "Most Preon kills wins";

        public override TaskType type { get; } = TaskType.PreonEvent;
        protected override string name { get; } = "Preon Event";


        int[] kills;
        int preonIndex = -1;

        public override bool CanActivate(int numPlayers)
        {
            return numPlayers > 1;
        }

        public override string GetDescription()
        {
            return description;
        }

        public override string GetWinMessage(int winningPlayer)
        {
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by getting {GetStylizedTaskWinStat(kills[winningPlayer].ToString())} kills with the Preon Accumulator.";
        }

        protected override void SetHooks(int numPlayers)
        {
            UnityEngine.Debug.Log($"Set Hooks in PreonEvent. {numPlayers} players");

            base.SetHooks(numPlayers);

            preonIndex = ProjectileCatalog.FindProjectileIndex("BeamSphere");

            GlobalEventManager.onCharacterDeathGlobal += OnKill;

            if (kills is null || kills.Length != numPlayers)
            {
                kills = new int[numPlayers];
            }
            Reset();

            TasksPlugin.StartPreonEvent();
        }

        protected override void StageEnd()
        {
            base.StageEnd();

            Evaluate();
            // seems like an unhook, but if everyone is dead, it doesn't need to run so it's w/e
            TasksPlugin.EndPreonEvent();
            Reset();
        }

        protected override void Unhook()
        {
            GlobalEventManager.onCharacterDeathGlobal -= OnKill;

            base.Unhook();
        }

        protected void UpdateProgress()
        {
            int mostKills = 0;
            for (int i = 0; i < kills.Length; i++)
            {
                if(kills[i] > mostKills)
                {
                    mostKills = kills[i];
                }
            }
            if (mostKills > 0)
            {
                for (int i = 0; i < progress.Length; i++)
                {
                    progress[i] = (float)kills[i] / mostKills;
                }
            }
            else
            {
                for (int i = 0; i < progress.Length; i++)
                {
                    progress[i] = 0; // shouldn't ever need this
                }
            }
            base.UpdateProgress(progress);
        }

        public void OnKill(DamageReport damageReport)
        {
            if (damageReport is null) return;
            if (damageReport.attackerMaster is null) return;
            if (damageReport.attackerMaster.playerCharacterMasterController is null) return;

            // Did I kill it?
            int playerNum = TasksPlugin.GetPlayerNumber(damageReport.attackerMaster);

            if (ProjectileCatalog.GetProjectileIndex(damageReport.damageInfo.inflictor) == preonIndex)
            {
                // if preon
                //Chat.AddMessage("Preon Kill");
                kills[playerNum]++;
                UpdateProgress();
            }
        }

        void Evaluate()
        {
            int mostKills = 0;
            int winner = 0;

            for (int i = 0; i < kills.Length; i++)
            {
                if(kills[i] > mostKills)
                {
                    mostKills = kills[i];
                    winner = i;
                }
            }
            Chat.AddMessage($"Player {winner} won Preon event with {mostKills} kills");
            CompleteTask(winner);
            
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
