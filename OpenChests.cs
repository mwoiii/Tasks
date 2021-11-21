using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;

namespace Tasks
{
    class OpenChests : Task
    {
        protected new string description { get; } = "First to open 5 chests wins (multishops count too)";
        /*
        public override string AchievementIdentifier { get; } = "SOLRUN_TASKS_OPEN_CHESTS_ACHIEVEMENT_ID"; // delete this from XML if there
        public override string UnlockableIdentifier { get; } = "SOLRUN_TASKS_OPEN_CHESTS_REWARD_ID"; // Delete me from XML too
        // I think all this does is hide it in the log until you have the prereq. You could still complete it (except most prereqs seem to be characters)
        public override string PrerequisiteUnlockableIdentifier { get; } = "";
        public override string AchievementNameToken { get; } = "SOLRUN_TASKS_OPEN_CHESTS_ACHIEVEMENT_NAME"; // Fine to have in the XML
        public override string AchievementDescToken { get; } = description; // plain English
        public override string UnlockableNameToken { get; } = ""; // plain English
        */
        public override TaskType type { get; } = TaskType.OpenChests;
        protected override string name { get; } = "Open 5 Chests";

        int[] chestsOpened;
        int numToOpen = 5;

        public override string GetDescription()
        {
            return description;
        }

        public override string GetWinMessage(int winningPlayer)
        {
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by opening {GetStylizedTaskWinStat(numToOpen.ToString())} chests first.";
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set Hooks in OpenChests. {numPlayers} players");

            base.SetHooks(numPlayers);


            GlobalEventManager.OnInteractionsGlobal += ChestsOpened;

            chestsOpened = new int[numPlayers];
            Reset();
        }

        protected override void Unhook()
        {
            GlobalEventManager.OnInteractionsGlobal -= ChestsOpened;

            Reset();

            base.Unhook();
        }

        protected void UpdateProgress()
        {
            for (int i = 0; i < progress.Length; i++)
            {
                progress[i] = (float)chestsOpened[i] / numToOpen;
            }
            base.UpdateProgress(progress);
        }

        void ChestsOpened(Interactor interactor, IInteractable interactable, GameObject go)
        {
            // who interacted
            int player = 0;
            for (int i = 0; i < chestsOpened.Length; i++)
            {
                if(TasksPlugin.GetPlayerCharacterMaster(i).GetBody().GetComponent<Interactor>() == interactor)
                {
                    player = i;
                }
            }
            // was it a chest or a multishop
            if(go?.GetComponent<ChestBehavior>())
            {
                chestsOpened[player]++;
            }
            else if(go?.GetComponent<ShopTerminalBehavior>())
            {
                if(go.name.Contains("MultiShop"))
                {
                    chestsOpened[player]++;
                }
            }
            UpdateProgress();

            if (IsComplete(player))
            {
                //Chat.AddMessage($"Player {player} Completed OpenChests");
                CompleteTask(player);
            }

        }

        protected override bool IsComplete(int playerNum)
        {
            return chestsOpened[playerNum] >= numToOpen;
        }

        void Reset()
        {
            if (chestsOpened is null)
                return;
            for (int i = 0; i < chestsOpened.Length; i++)
            {
                chestsOpened[i] = 0;
            }
            ResetProgress();
        }
    }
}
