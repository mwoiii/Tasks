using System;
using System.Collections.Generic;
using System.Text;
using RoR2;

namespace Tasks
{
    class DontUseSkill : Task
    {
        protected new string description { get; } = "Last to use their utility skill wins";
        /*
        public override string AchievementIdentifier { get; } = "SOLRUN_TASKS_BAD_SKILL_ACHIEVEMENT_ID"; // delete this from XML if there 
        public override string UnlockableIdentifier { get; } = "SOLRUN_TASKS_BAD_SKILL_REWARD_ID"; // Delete me from XML too
        // XML: C:\Program Files (x86)\Steam\userdata\Some Numbers\632360\remote\UserProfiles\MoreNumbers.xml
        // I think all this does is hide it in the log until you have the prereq. You could still complete it (except most prereqs seem to be characters)
        public override string PrerequisiteUnlockableIdentifier { get; } = "";
        public override string AchievementNameToken { get; } = "SOLRUN_TASKS_BAD_SKILL_ACHIEVEMENT_NAME"; // Fine to have in the XML
        public override string AchievementDescToken { get; } = description; // plain English
        public override string UnlockableNameToken { get; } = ""; // plain English
        */
        public override TaskType type { get; } = TaskType.BadSkill;
        protected override string name { get; } = "Bad Skill";

        SkillSlot badSkill;
        bool[] playerFailed;
        int numPlayersFailed;
        bool active = false;

        public override bool CanActivate(int numPlayers)
        {
            return numPlayers > 1;
        }

        public override string GetDescription()
        {
            return description;
        }

        protected override void SetHooks(int numPlayers)
        {
            Chat.AddMessage($"Set Hooks in DontUseSkill. {numPlayers} players");

            base.SetHooks(numPlayers);

            badSkill = SkillSlot.Utility;
            playerFailed = new bool[numPlayers];
            active = true;
            ResetAll();

            TasksPlugin.OnAbilityUsed += AbilityUsed;
        }

        protected override void Unhook()
        {
            if (!active)
                return;
            active = false;
            // are there still 2+ players that haven't failed at the end of the stage?
            // give each of them the reward
            // this might be kind of broken
            // I think it will just try to hide the UI like twice for each winner
            // This doesn't work for single player. Less work to make this task not generate if only 1 player (bc I'm planning on doing that anyway)
            if(totalNumberPlayers - numPlayersFailed > 1)
            {
                for (int i = 0; i < playerFailed.Length; i++)
                {
                    if(!playerFailed[i])
                    {
                        CompleteTask(i);
                    }
                }
            }


            TasksPlugin.OnAbilityUsed -= AbilityUsed;
            ResetAll();

            base.Unhook();
        }

        void AbilityUsed(int playerNum, SkillSlot slot)
        {
            if(slot == badSkill)
            {
                if (playerFailed[playerNum])
                    return;
                Chat.AddMessage($"Player {playerNum} failed BadSkill");
                playerFailed[playerNum] = true;
                numPlayersFailed++;
                // have all but one player failed?
                if(numPlayersFailed >= totalNumberPlayers-1)
                {
                    for (int i = 0; i < playerFailed.Length; i++)
                    {
                        // figure out who hasn't failed yet
                        if(!playerFailed[i])
                        {
                            CompleteTask(i);
                            return;
                        }
                    }
                }
            }
        }

        void ResetAll()
        {
            if (playerFailed is null)
                return;
            for (int i = 0; i < playerFailed.Length; i++)
            {
                playerFailed[i] = false;
            }
        }
    }
}
