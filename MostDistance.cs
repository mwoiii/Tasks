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
        /*
        public override string AchievementIdentifier { get; } = "SOLRUN_TASKS_MOST_DISTANCE_ACHIEVEMENT_ID"; // delete this from XML if there 
        public override string UnlockableIdentifier { get; } = "SOLRUN_TASKS_MOST_DISTANCE_REWARD_ID"; // Delete me from XML too
        // XML: C:\Program Files (x86)\Steam\userdata\Some Numbers\632360\remote\UserProfiles\MoreNumbers.xml
        // I think all this does is hide it in the log until you have the prereq. You could still complete it (except most prereqs seem to be characters)
        public override string PrerequisiteUnlockableIdentifier { get; } = "";
        public override string AchievementNameToken { get; } = "SOLRUN_TASKS_MOST_DISTANCE_ACHIEVEMENT_NAME"; // Fine to have in the XML
        public override string AchievementDescToken { get; } = description; // plain English
        public override string UnlockableNameToken { get; } = ""; // plain English
        */
        public override TaskType type { get; } = TaskType.MostDistance;
        protected override string name { get; } = "Most Distance";

        double[] startDistances;
        bool active = false;

        public override bool CanActivate(int numPlayers)
        {
            return numPlayers > 1;
        }

        public override string GetDescription()
        {
            return description;
        }

        protected override void SetHooks(int numPlayers)
        {
            Chat.AddMessage($"Set Hooks in MostDistance. {numPlayers} players");

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

            active = true;
        }

        protected override void Unhook()
        {
            if (!active)
                return;
            Chat.AddMessage("Unhook MostDistance. This should only run once");
            active = false;

            Evaluate();

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

            CompleteTask(winner);
        }
    }
}
