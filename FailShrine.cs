using System;
using System.Collections.Generic;
using System.Text;
using RoR2;

namespace Tasks
{
    class FailShrine : Task
    {
        protected new string description { get; } = "First to Fail";
        /*
        public override string AchievementIdentifier { get; } = "SOLRUN_TASKS_FAIL_SHRINE_ACHIEVEMENT_ID"; // delete this from XML if there 
        public override string UnlockableIdentifier { get; } = "SOLRUN_TASKS_FAIL_SHRINE_REWARD_ID"; // Delete me from XML too
        // XML: C:\Program Files (x86)\Steam\userdata\Some Numbers\632360\remote\UserProfiles\MoreNumbers.xml
        // I think all this does is hide it in the log until you have the prereq. You could still complete it (except most prereqs seem to be characters)
        public override string PrerequisiteUnlockableIdentifier { get; } = "";
        public override string AchievementNameToken { get; } = "SOLRUN_TASKS_FAIL_SHRINE_ACHIEVEMENT_NAME"; // Fine to have in the XML
        public override string AchievementDescToken { get; } = description; // plain English
        public override string UnlockableNameToken { get; } = ""; // plain English
        */
        public override TaskType type { get; } = TaskType.FailShrine;
        protected override string name { get; } = "Fail a Chance Shrine";

        public override string GetDescription()
        {
            return description;
        }

        protected override void SetHooks(int numPlayers)
        {
            UnityEngine.Debug.Log($"Set Hooks in FailShrine. {numPlayers} players");

            base.SetHooks(numPlayers);

            ShrineChanceBehavior.onShrineChancePurchaseGlobal += this.OnShrineChancePurchase;

        }

        protected override void Unhook()
        {
            ShrineChanceBehavior.onShrineChancePurchaseGlobal -= this.OnShrineChancePurchase;

            base.Unhook();
        }

        void OnShrineChancePurchase(bool failed, Interactor interactor)
        {
            if (failed)
            {
                for (int i = 0; i < totalNumberPlayers; i++)
                {
                    if (TasksPlugin.GetPlayerCharacterMaster(i).GetBody().GetComponent<Interactor>() == interactor)
                    {
                        //Chat.AddMessage($"Player {i} Completed FailShrine");
                        CompleteTask(i);
                        return;
                    }
                }
            }
        }

    }
}
