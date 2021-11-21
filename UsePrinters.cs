using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;

namespace Tasks
{
    class UsePrinters : Task
    {
        protected new string description { get; } = "Use 2 Different 3D Printers";

        public override TaskType type { get; } = TaskType.UsePrinters;
        protected override string name { get; } = "Use Printers";

        HashSet<GameObject>[] printersUsed;
        int numToUse = 2;
        // I would like to be able to check if there are at least this many printers on the stage

        public override string GetDescription()
        {
            return description;
        }

        public override string GetWinMessage(int winningPlayer)
        {
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by using {GetStylizedTaskWinStat(numToUse.ToString())} different 3D printers first.";
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set Hooks in StartTele. {numPlayers} players");

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

        void UpdateProgress()
        {
            for (int i = 0; i < progress.Length; i++)
            {
                progress[i] = (float)printersUsed[i].Count / numToUse;
            }
            base.UpdateProgress(progress);
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
                    UpdateProgress();
                    if (IsComplete(player))
                    {
                        //Chat.AddMessage($"Player {player} Completed UsePrinters");
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
            ResetProgress();
        }
    }
}
