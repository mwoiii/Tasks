using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tasks
{
    class BiggestHit : Task
    {
        public static new string description { get; } = "Biggest hit wins";

        public override string AchievementIdentifier { get; } = "SOLRUN_TASKS_BIGGEST_HIT_ACHIEVEMENT_ID"; // delete this from XML if there 
        public override string UnlockableIdentifier { get; } = "SOLRUN_TASKS_BIGGEST_HIT_REWARD_ID"; // Delete me from XML too
        // XML: C:\Program Files (x86)\Steam\userdata\Some Numbers\632360\remote\UserProfiles\MoreNumbers.xml
        // I think all this does is hide it in the log until you have the prereq. You could still complete it (except most prereqs seem to be characters)
        public override string PrerequisiteUnlockableIdentifier { get; } = "";
        public override string AchievementNameToken { get; } = "SOLRUN_TASKS_BIGGEST_HIT_ACHIEVEMENT_NAME"; // Fine to have in the XML
        public override string AchievementDescToken { get; } = description; // plain English
        public override string UnlockableNameToken { get; } = ""; // plain English

        protected override TaskType type { get; } = TaskType.BiggestHit;
        protected override string name { get; } = "Biggest Hit";

        // by it's nature, this task gets triggered at the end.
        // How can I make it not give out temp items that will just get removed?
        // maybe they are granted for the next map?
        // as it is, end of game is called after remove temp items
        // so it should just grant them for the next stage
        // ya, it does work like that. You lose the items at the end of the next stage

        // This task doesn't actually make much sense. Most abilitites do a set amount of damage.
        // Whomever has the biggest ability wins every time
        // would need to be biggest ability combined with all of the effects like ukulele and bands, etc
        // Most damage within 0.2s??

        float[] biggestHit;
        bool active = false;

        protected override void SetHooks(int numPlayers)
        {
            Chat.AddMessage($"Set Hooks in BiggestHit. {numPlayers} players");

            base.SetHooks(numPlayers);

            GlobalEventManager.onServerDamageDealt += OnDamage;
            
            //TeleporterInteraction.onTeleporterFinishGlobal += Evaluate;

            if (biggestHit is null || biggestHit.Length != numPlayers)
            {
                biggestHit = new float[numPlayers];
            }
            Reset();
            active = true;
        }

        protected override void Unhook()
        {
            if (!active)
                return;
            Chat.AddMessage("Unhook BiggestHit. This should only run once");
            active = false;
            
            Evaluate();

            GlobalEventManager.onServerDamageDealt -= OnDamage;

            Reset();

            base.Unhook();
        }

        public void OnDamage(DamageReport report)
        {
            if (report is null) return;
            if (report.attackerMaster is null) return;
            if (report.attackerMaster.playerCharacterMasterController is null) return;

            // Did I hit it?
            int playerNum = TasksPlugin.GetPlayerNumber(report.attackerMaster);
            
            float damage = report.damageDealt;
            if(damage > biggestHit[playerNum])
            {
                Chat.AddMessage($"Set new big hit for player {playerNum}. {biggestHit[playerNum]} -> {damage}");
                biggestHit[playerNum] = damage;
            }
        }

        void Evaluate()
        {
            float biggest = 0;
            int bigPlayer = 0;

            for (int i = 0; i < biggestHit.Length; i++)
            {
                if(biggestHit[i] > biggest)
                {
                    biggest = biggestHit[i];
                    bigPlayer = i;
                }
            }

            CompleteTask(bigPlayer);
        }

        void Reset()
        {
            if (biggestHit is null)
                return;

            for (int i = 0; i < biggestHit.Length; i++)
            {
                biggestHit[i] = 0;
            }

        }
    }
}