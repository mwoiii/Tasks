using System;
using System.Collections.Generic;
using System.Text;
using RoR2;

namespace Tasks
{
    class OrderedSkills : Task
    {
        protected new string description { get; } = "Use skills in a specific order";
        /*
        public override string AchievementIdentifier { get; } = "SOLRUN_TASKS_ORDERED_SKILLS_ACHIEVEMENT_ID"; // delete this from XML if there 
        public override string UnlockableIdentifier { get; } = "SOLRUN_TASKS_ORDERED_SKILLS_REWARD_ID"; // Delete me from XML too
        // XML: C:\Program Files (x86)\Steam\userdata\Some Numbers\632360\remote\UserProfiles\MoreNumbers.xml
        // I think all this does is hide it in the log until you have the prereq. You could still complete it (except most prereqs seem to be characters)
        public override string PrerequisiteUnlockableIdentifier { get; } = "";
        public override string AchievementNameToken { get; } = "SOLRUN_TASKS_ORDERED_SKILLS_ACHIEVEMENT_NAME"; // Fine to have in the XML
        public override string AchievementDescToken { get; } = description; // plain English
        public override string UnlockableNameToken { get; } = ""; // plain English
        */
        public override TaskType type { get; } = TaskType.OrderedSkills;
        protected override string name { get; } = "Ordered Skills";

        SkillSlot[] order;
        int[] whereInOrder;

        public override string GetDescription()
        {
            return description;
        }

        protected override void SetHooks(int numPlayers)
        {
            Chat.AddMessage($"Set Hooks in OrderedSkills. {numPlayers} players");

            base.SetHooks(numPlayers);

            order = new SkillSlot[] { SkillSlot.Primary, SkillSlot.Secondary, SkillSlot.Utility, SkillSlot.Special};
            whereInOrder = new int[numPlayers];
            ResetAll();

            TasksPlugin.OnAbilityUsed += AbilityUsed;
        }

        protected override void Unhook()
        {
            TasksPlugin.OnAbilityUsed -= AbilityUsed;
            ResetAll();

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

        void ResetAll()
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
