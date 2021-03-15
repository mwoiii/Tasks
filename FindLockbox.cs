using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tasks
{
    class FindLockbox : Task
    {
        public override TaskType type { get; } = TaskType.FindLockbox;


        public override bool CanActivate(int numPlayers)
        {
            int rustyKeys = 0;
            foreach (CharacterMaster characterMaster in CharacterMaster.readOnlyInstancesList)
            {
                rustyKeys += characterMaster.inventory.GetItemCount(ItemIndex.TreasureCache);
            }
            
            return (rustyKeys > 0);
        }

        public override string GetDescription()
        {
            return "Find the Lockbox";
        }

        protected override void SetHooks(int numPlayers)
        {
            base.SetHooks(numPlayers);

            GlobalEventManager.OnInteractionsGlobal += ChestsOpened;

        }

        protected override void Unhook()
        {
            GlobalEventManager.OnInteractionsGlobal -= ChestsOpened;


            base.Unhook();
        }

        void ChestsOpened(Interactor interactor, IInteractable interactable, GameObject go)
        {
            // who interacted
            int player = 0;
            for (int i = 0; i < totalNumberPlayers; i++)
            {
                if (TasksPlugin.GetPlayerCharacterMaster(i).GetBody().GetComponent<Interactor>() == interactor)
                {
                    player = i;
                }
            }
            
            if(go?.GetComponent<ChestBehavior>())
            {
                if(go.name.Contains("Lockbox"))
                {
                    CompleteTask(player);
                }
            }
        }
    }
}
