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
        /*
        public override string AchievementIdentifier { get; } = "SOLRUN_TASKS_PREON_EVENT_ACHIEVEMENT_ID"; // delete this from XML if there 
        public override string UnlockableIdentifier { get; } = "SOLRUN_TASKS_PREON_EVENT_REWARD_ID"; // Delete me from XML too
        // XML: C:\Program Files (x86)\Steam\userdata\Some Numbers\632360\remote\UserProfiles\MoreNumbers.xml
        // I think all this does is hide it in the log until you have the prereq. You could still complete it (except most prereqs seem to be characters)
        public override string PrerequisiteUnlockableIdentifier { get; } = "";
        public override string AchievementNameToken { get; } = "SOLRUN_TASKS_PREON_EVENT_ACHIEVEMENT_NAME"; // Fine to have in the XML
        public override string AchievementDescToken { get; } = description; // plain English
        public override string UnlockableNameToken { get; } = ""; // plain English
        */
        public override TaskType type { get; } = TaskType.PreonEvent;
        protected override string name { get; } = "Preon Event";


        int[] kills;
        bool active;

        int preonIndex = -1;

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
            Chat.AddMessage($"Set Hooks in PreonEvent. {numPlayers} players");

            base.SetHooks(numPlayers);

            //ProjectileCatalog.GetProjectilePrefab(0).projectileNames
            //ClassicStageInfo s = new ClassicStageInfo();
            //var thing = s.GetFieldValue<string>("someString");

            /*
            string[] names;
            // This is how you access a private static array in a static class
            names = typeof(ProjectileCatalog).GetFieldValue<string[]>("projectileNames");
            if (names != null)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    Chat.AddMessage(names[i]);
                }
            }
            */

            preonIndex = ProjectileCatalog.FindProjectileIndex("BeamSphere");

            GlobalEventManager.onCharacterDeathGlobal += OnKill;
            //GlobalEventManager.onServerDamageDealt += OnDamage;

            if (kills is null || kills.Length != numPlayers)
            {
                kills = new int[numPlayers];
            }
            Reset();
            active = true;

            TasksPlugin.StartPreonEvent();
        }

        protected override void Unhook()
        {
            if (!active)
                return;
            active = false;

            Evaluate();

            GlobalEventManager.onCharacterDeathGlobal -= OnKill;
            //GlobalEventManager.onServerDamageDealt -= OnDamage;

            TasksPlugin.EndPreonEvent();

            Reset();

            base.Unhook();
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
                // update all clients about leaderboards
                // data = kills
                //OnUpdate(type, data);
                // would a client be able to tell "I'm player 1 and player 1 is winning, therefore, I'm winning"
                // or would they jsut know player 1 is winning and not know what their player num is?
                // should I update every time someone gets a kill? Every 1 sec? Every 5 kills? Every kill, but not more often than every second?
                // But I don't want to send info on the first kill, then miss the 2nd and 3rd kill that happened 0.1s later. 
                // Would rather skip the first kill and report after the 3rd. 
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
        }
    }
}
