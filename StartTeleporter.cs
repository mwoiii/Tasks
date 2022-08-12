using System;
using System.Collections;
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

        float soloTimer;
        float soloTimeAmountSec = 90;
        bool hasFailed = false;
        bool succeeded = false;

        public override bool CanActivate(int numPlayers)
        {
            return true; // numPlayers > 1;
        }

        public override string GetDescription()
        {
            return description;
        }

        public override string GetWinMessage(int winningPlayer)
        {
            if(winningPlayer < 0)
            {
                return $"You failed {GetStylizedTaskName(name)} by not getting to the {GetStylizedTaskWinStat("teleporter")} in {GetStylizedTaskWinStat(soloTimeAmountSec.ToString())} seconds.";
            }
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by triggering the {GetStylizedTaskWinStat("teleporter")} first.";
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set Hooks in StartTele. {numPlayers} players");

            base.SetHooks(numPlayers);

            GlobalEventManager.OnInteractionsGlobal += OnInteraction;

            if(numPlayers == 1)
            {
                StartSolo();
            }
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
                Evaluate(player);
                
            }
        }

        void StartSolo()
        {
            if(TasksPlugin.instance)
            {
                TasksPlugin.instance.StartCoroutine(Countdown());
            }
        }

        IEnumerator Countdown()
        {
            hasFailed = false;
            succeeded = false;
            soloTimer = soloTimeAmountSec;

            for (int i = 0; i < soloTimeAmountSec; i++)
            {
                // this probably keeps counting if you press esc tp pause
                // but maybe it doesn't. Either way, oh well
                yield return new WaitForSeconds(1);
                soloTimer -= 1;
                UpdateProgressSolo();
            }
            hasFailed = true;
            if(!succeeded)
            {
                // failure case is -1
                CompleteTask(-1);
            }
        }

        void Evaluate(int playerNum)
        {
            if(totalNumberPlayers > 1)
            {
                CompleteTask(playerNum);
                return;
            }

            if(!hasFailed)
            {
                succeeded = true;
                CompleteTask(playerNum);
            }

        }

        void UpdateProgressSolo()
        {
            if (succeeded)
            {
                // full bar just clutters it up, so just empty it
                progress[0] = 0;
            }
            else
            {
                progress[0] = Mathf.Max(0, soloTimer / soloTimeAmountSec);
            }

            base.UpdateProgress(progress);
        }

        
    }
}
