using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;

namespace Tasks
{
    class HealingItem : Task
    {

        public override TaskType type { get; } = TaskType.HealingItem;
        protected override string name { get; } = "Find a healing item";


        public override string GetDescription()
        {
            return "Find a healing item";
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set Hooks in HealingItem. {numPlayers} players");

            base.SetHooks(numPlayers);

            //RoR2.GlobalEventManager
            Inventory.onServerItemGiven += OnItemGained;
            // +=
        }

        protected override void Unhook()
        {
            // -=
            Inventory.onServerItemGiven -= OnItemGained;

            base.Unhook();
        }

        void OnItemGained(Inventory inventory, ItemIndex item, int count)
        {
            //RoR2.ItemTag.Healing
            if (ItemCatalog.GetItemDef(item).ContainsTag(ItemTag.Healing))
            {
                // it's a healing item
                for (int i = 0; i < totalNumberPlayers; i++)
                {
                    // charactermaster requires an inventory component
                    if(TasksPlugin.GetPlayerCharacterMaster(i).inventory == inventory)
                    {
                        CompleteTask(i);
                        return;
                    }
                }
            }
        }

    }
}
