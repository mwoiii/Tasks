using System;
using System.Collections.Generic;
using System.Text;
using RoR2;

namespace Tasks
{
    class FailShrine : Task
    {
        protected new string description { get; } = "First to Fail";

        public override TaskType type { get; } = TaskType.FailShrine;
        protected override string name { get; } = "Fail a Chance Shrine";

        public override string GetDescription()
        {
            return description;
        }

        public override string GetWinMessage(int winningPlayer)
        {
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by {GetStylizedTaskWinStat("failing")} a chance shrine.";
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
