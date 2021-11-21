using System;
using System.Collections.Generic;
using System.Text;
using RoR2;

namespace Tasks
{
    class OrderedSkills : Task
    {
        protected new string description { get; } = "Use skills in order";

        public override TaskType type { get; } = TaskType.OrderedSkills;
        protected override string name { get; } = "Ordered Skills";

        SkillSlot[] order;
        int[] whereInOrder;

        public override string GetDescription()
        {
            return description;
        }

        public override string GetWinMessage(int winningPlayer)
        {
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by using their abilities in order.";
        }

        protected override void SetHooks(int numPlayers)
        {
            UnityEngine.Debug.Log($"Set Hooks in OrderedSkills. {numPlayers} players");

            base.SetHooks(numPlayers);

            order = new SkillSlot[] { SkillSlot.Primary, SkillSlot.Secondary, SkillSlot.Utility, SkillSlot.Special};
            whereInOrder = new int[numPlayers];
            Reset();

            TasksPlugin.OnAbilityUsed += AbilityUsed;
        }

        protected override void Unhook()
        {
            TasksPlugin.OnAbilityUsed -= AbilityUsed;
            Reset();

            base.Unhook();
        }

        void AbilityUsed(int playerNum, SkillSlot slot)
        {
            int index = whereInOrder[playerNum];
            if(order[index] == slot)
            {
                // used the right skill
                whereInOrder[playerNum]++;
                if(whereInOrder[playerNum] >= order.Length)
                {
                    CompleteTask(playerNum);
                }
            }
            else
            {
                whereInOrder[playerNum] = 0;
            }
        }

        void Reset()
        {
            if (whereInOrder is null)
                return;
            for (int i = 0; i < whereInOrder.Length; i++)
            {
                whereInOrder[i] = 0;
            }
        }
    }
}
