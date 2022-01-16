using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;

namespace Tasks
{
    class QuickDraw : Task
    {
        public override TaskType type { get; } = TaskType.QuickDraw;

        protected override string name { get; } = "Quick Draw";

        public override string GetDescription()
        {
            return "Kill an enemy within 3s of spawning."; // reword this?
        }

        public override string GetWinMessage(int winningPlayer)
        {
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by killing an enemy within {GetStylizedTaskWinStat("3")}s of spawning.";
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set Hooks in KillStreak. {numPlayers} players");

            base.SetHooks(numPlayers);

            GlobalEventManager.onCharacterDeathGlobal += OnKill;


        }

        protected override void Unhook()
        {

            GlobalEventManager.onCharacterDeathGlobal -= OnKill;


            base.Unhook();
        }

        void OnKill(DamageReport report)
        {
            if (report.attackerMaster.playerCharacterMasterController is null) return;
            float time = Run.FixedTimeStamp.now.t - report.victimBody.localStartTime.t;
            //Debug.Log($"Player killed something. Alive: {report.victimBody.localStartTime.t} CurrentTime: {Run.FixedTimeStamp.now.t} Diff: {time}");
            if (time < 3)
            {
                int playerNum = TasksPlugin.GetPlayerNumber(report.attackerMaster);
                CompleteTask(playerNum);

            }
        }

    }
}
