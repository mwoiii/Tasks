using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;

namespace Tasks
{
    class StartTeleporter : Task
    {
        protected new string description { get; } = "Better be First";

        public override TaskType type { get; } = TaskType.StartTele;
        protected override string name { get; } = "Activate the Teleporter";

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
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by triggering the {GetStylizedTaskWinStat("teleporter")} first.";
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set Hooks in StartTele. {numPlayers} players");

            base.SetHooks(numPlayers);

            GlobalEventManager.OnInteractionsGlobal += OnInteraction;

        }

        protected override void Unhook()
        {
            GlobalEventManager.OnInteractionsGlobal -= OnInteraction;

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

            if (go?.GetComponent<TeleporterInteraction>())
            {
                CompleteTask(player);
            }
        }
    }
}
