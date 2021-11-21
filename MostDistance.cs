using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using RoR2.Stats;

namespace Tasks
{
    class MostDistance : Task
    {
        protected string description { get; } = "Most distance wins";

        public override TaskType type { get; } = TaskType.MostDistance;
        protected override string name { get; } = "Most Distance";

        double[] startDistances;
        double winnerDist;

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
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by covering the most ({GetStylizedTaskWinStat(winnerDist.ToString())}m) distance.";
        }

        protected override void SetHooks(int numPlayers)
        {
            UnityEngine.Debug.Log($"Set Hooks in MostDistance. {numPlayers} players");

            base.SetHooks(numPlayers);


            if (startDistances is null || startDistances.Length != numPlayers)
            {
                startDistances = new double[numPlayers];
            }

            for (int i = 0; i < startDistances.Length; i++)
            {
                StatSheet s = TasksPlugin.GetPlayerCharacterMaster(i).playerStatsComponent.currentStats;
                startDistances[i] = s.GetStatValueDouble(StatDef.totalDistanceTraveled);
            }

        }

        protected override void StageEnd()
        {
            base.StageEnd();

            Evaluate();
        }

        protected override void Unhook()
        {

            base.Unhook();
        }

        void Evaluate()
        {
            double mostDist = 0;
            int winner = 0;


            for (int i = 0; i < startDistances.Length; i++)
            {
                // could have used current stats instead of global stats (this run vs all runs)
                // from Complete3StagesWithoutHealing achieve
                // StatSheet currentStats = base.localUser.currentNetworkUser.masterPlayerStatsComponent.currentStats;
                // if (sceneDefForCurrentScene.stageOrder >= 3 && currentStats.GetStatValueULong(StatDef.totalHealthHealed)
                StatSheet s = TasksPlugin.GetPlayerCharacterMaster(i).playerStatsComponent.currentStats;
                double endDist = s.GetStatValueDouble(StatDef.totalDistanceTraveled);
                double distDelta = endDist - startDistances[i];
                if(distDelta > mostDist)
                {
                    mostDist = distDelta;
                    winner = i;
                }
                Chat.AddMessage($"MostDist({i}): {startDistances[i]} -> {endDist} = {distDelta}. Winner: {winner} with {mostDist}");
            }
            winnerDist = mostDist;
            CompleteTask(winner);
        }
    }
}
