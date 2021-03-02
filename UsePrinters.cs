using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;

namespace Tasks
{
    class UsePrinters : Task
    {
        public static new string description { get; } = "Use 2 Different 3D Printers";
        /*
        public override string AchievementIdentifier { get; } = "SOLRUN_TASKS_USE_PRINTERS_ACHIEVEMENT_ID"; // delete this from XML if there 
        public override string UnlockableIdentifier { get; } = "SOLRUN_TASKS_USE_PRINTERS_REWARD_ID"; // Delete me from XML too
        // XML: C:\Program Files (x86)\Steam\userdata\Some Numbers\632360\remote\UserProfiles\MoreNumbers.xml
        // I think all this does is hide it in the log until you have the prereq. You could still complete it (except most prereqs seem to be characters)
        public override string PrerequisiteUnlockableIdentifier { get; } = "";
        public override string AchievementNameToken { get; } = "SOLRUN_TASKS_USE_PRINTERS_ACHIEVEMENT_NAME"; // Fine to have in the XML
        public override string AchievementDescToken { get; } = description; // plain English
        public override string UnlockableNameToken { get; } = ""; // plain English
        */
        public override TaskType type { get; } = TaskType.UsePrinters;
        protected override string name { get; } = "Use Printers";

        HashSet<GameObject>[] printersUsed;
        int numToUse = 2; 
        // I would like to be able to check if there are at least this many printers on the stage

        protected override void SetHooks(int numPlayers)
        {
            Chat.AddMessage($"Set Hooks in StartTele. {numPlayers} players");

            base.SetHooks(numPlayers);

            GlobalEventManager.OnInteractionsGlobal += OnInteraction;
            
            printersUsed = new HashSet<GameObject>[numPlayers];
            for (int i = 0; i < printersUsed.Length; i++)
            {
                printersUsed[i] = new HashSet<GameObject>();
            }
        }

        protected override void Unhook()
        {
            GlobalEventManager.OnInteractionsGlobal -= OnInteraction;
            Reset();
            base.Unhook();
        }

        void OnInteraction(Interactor interactor, IInteractable interactable, GameObject go)
        {
            int player = 0;
            for (int i = 0; i < totalNumberPlayers; i++)
            {
                if (TasksPlugin.GetPlayerCharacterMaster(i).GetBody().GetComponent<Interactor>() == interactor)
                {
                    player = i;
                }
            }

            if (go?.GetComponent<ShopTerminalBehavior>())
            {
                if (go.name.Contains("Duplicator"))
                {
                    if (printersUsed[player].Contains(go))
                        return;
                    printersUsed[player].Add(go);
                    if (IsComplete(player))
                    {
                        Chat.AddMessage($"Player {player} Completed UsePrinters");
                        CompleteTask(player);
                    }
                }
            }
        }

        protected override bool IsComplete(int playerNum)
        {
            return printersUsed[playerNum].Count >= numToUse;
        }
        void Reset()
        {
            if (printersUsed is null)
                return;
            for (int i = 0; i < printersUsed.Length; i++)
            {
                printersUsed[i].Clear();
            }
        }
    }
}
