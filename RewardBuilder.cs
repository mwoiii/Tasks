using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using R2API;

namespace Tasks
{
    class RewardBuilder
    {

        public static Reward CreateRandomReward()
        {

            // ==== Type of Reward
            WeightedSelection<RewardType> typeSelection = new WeightedSelection<RewardType>();

            typeSelection.AddChoice(RewardType.Item, 2);
            typeSelection.AddChoice(RewardType.TempItem, 1);
            typeSelection.AddChoice(RewardType.Command, 0.5f);

            RewardType type = typeSelection.Evaluate(UnityEngine.Random.value);


            // ===== Item
            // After update. This should be all I need
            // I think this will give me the list of white items 80% of the time and the list of green items 20% of the time (or whatevs the rates are)
            // 0.8, 0.2, 0.01
            var smallChestList = Run.instance.smallChestDropTierSelector.Evaluate(UnityEngine.Random.value);
            PickupIndex smallItem = Run.instance.treasureRng.NextElementUniform<PickupIndex>(smallChestList);

            // randomly selecting a chest type isn;t even important. I never made themed tasks that reward a utily chest or a damage chest specifically.
            // I can change small/med/large chests for harder tasks
            /*
            // I'm p sure ItemDropLocation was an enum (it's not there anymore)
            WeightedSelection<ItemDropLocation> chestSelection = new WeightedSelection<ItemDropLocation>();

            // wiki says weights are small:24, med:4, large:2, specialty:2
            // specialty are so low just to be more rare, not bc they are too powerful
            // so I'm fine with increasing their weight
            chestSelection.AddChoice(ItemDropLocation.SmallChest, 24);
            chestSelection.AddChoice(ItemDropLocation.MediumChest, 4);
            chestSelection.AddChoice(ItemDropLocation.UtilityChest, 8);
            chestSelection.AddChoice(ItemDropLocation.DamageChest, 8);
            chestSelection.AddChoice(ItemDropLocation.HealingChest, 8);
            chestSelection.AddChoice(ItemDropLocation.LargeChest, 1); // legendary


            // Can I give players use items in the same way as I give them regular items?
            //chestSelection.AddChoice(ItemDropLocation.EquipmentChest, 20);

            // EntityStates.ScavMonster.FindItem uses random.value in evaluate. It returns a value between 0.0 and 1.0
            // multiplies this value by the total weight then figures out which selection that corresponds to
            // Using the example values of 24, 4, 8, 8, 8, 8, 1 = 61
            // 61 * value = 21(for example) would map to a small chest because 21 < 24
            // 43 would map to 24 +4 +8 =36 < 43 < 24+4+8+8 =44 so the damage chest
            ItemDropLocation chest = chestSelection.Evaluate(UnityEngine.Random.value);

            
            // special chests
            // chest behaviour is a monobehaviour so i can't do this I don't think
            //ChestBehavior c = new ChestBehavior();
            // I have no idea how RollItem uses the required tag
            //c.requiredItemTag = ItemTag.Damage;
            //c.RollItem();
            //c.dropPickup; // private


            // New way to get the list of items that can drop
            // then need to randomize them
            List<PickupIndex> tier1Drops = Run.instance.availableTier1DropList;

            // get a random item from a specific chest
            // and turn it into an itemIndex (as opposed to a pickupIndex which seems to be depreciated
            PickupIndex pickupIndex = ItemDropAPI.GetSelection(chest, UnityEngine.Random.value);
            //PickupDef def = PickupCatalog.GetPickupDef(pickupIndex);
            //ItemIndex item = def.itemIndex;
            */
            // ===== End Item

            //Chat.AddMessage($"Reward created: {pickupIndex:g} from a {chest:g}");

            Reward reward = new Reward(type, smallItem, (type == RewardType.TempItem) ? 5 : 1, false, 100, 100);
            return reward;
        }
        
    }

    public struct Reward
    {
        public Reward(RewardType _type, PickupIndex _item, int _numItems, bool _temporary, int _gold, int _xp)
        {
            type = _type;
            item = _item;
            numItems = _numItems;
            temporary = _temporary;
            gold = _gold;
            xp = _xp;
        }

        public RewardType type;
        public PickupIndex item;
        public int numItems;
        public bool temporary;
        public int gold;
        public int xp;

        public override string ToString() => $"{type:g}, {item:g}";
    }

    public struct TempItem
    {
        public TempItem(PickupIndex _item, int _count)
        {
            item = _item;
            count = _count;
        }
        public PickupIndex item;
        public int count;
    }

    public enum RewardType { Item, TempItem, Command, Gold, Xp };

}
