using System;
using System.Collections.Generic;
using System.Text;
using RoR2;

namespace Tasks
{
    class DontUseSkill : Task
    {
        protected new string description { get; } = "Last to use their utility skill wins";

        public override TaskType type { get; } = TaskType.BadSkill;
        protected override string name { get; } = "Bad Skill";

        SkillSlot badSkill;
        bool[] playerFailed;
        int numPlayersFailed;

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
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by not using their {GetStylizedTaskWinStat(badSkill.ToString())} skill.";
        }

        protected override void SetHooks(int numPlayers)
        {
            UnityEngine.Debug.Log($"Set Hooks in DontUseSkill. {numPlayers} players");

            base.SetHooks(numPlayers);

            badSkill = SkillSlot.Utility;
            playerFailed = new bool[numPlayers];
            Reset();

            TasksPlugin.OnAbilityUsed += AbilityUsed;
        }

        protected override void StageEnd()
        {
            base.StageEnd();

            Evaluate();
            Reset();
        }

        protected override void Unhook()
        {
            TasksPlugin.OnAbilityUsed -= AbilityUsed;

            base.Unhook();
        }

        void Evaluate()
        {
            // are there still 2+ players that haven't failed at the end of the stage?
            // give each of them the reward
            // this might be kind of broken
            // I think it will just try to hide the UI like twice for each winner
            // This doesn't work for single player. Less work to make this task not generate if only 1 player (bc I'm planning on doing that anyway)
            if (totalNumberPlayers - numPlayersFailed > 1)
            {
                for (int i = 0; i < playerFailed.Length; i++)
                {
                    if (!playerFailed[i])
                    {
                        UpdateProgressMultiWinner();
                        CompleteTask(i);
                    }
                }
            }
        }

        void UpdateProgress()
        {
            for (int i = 0; i < progress.Length; i++)
            {
                // if you've failed, your bar is 0
                // if you haven't, bar shows how many people have failed.
                // when it fills up, that means you won
                progress[i] = playerFailed[i]?0:(numPlayersFailed/(totalNumberPlayers-1));
            }
            base.UpdateProgress(progress);
        }

        void UpdateProgressMultiWinner()
        {
            for (int i = 0; i < progress.Length; i++)
            {
                // full bar if you won
                progress[i] = playerFailed[i] ? 0 : 1;
            }
            base.UpdateProgress(progress);
        }

        void AbilityUsed(int playerNum, SkillSlot slot)
        {
            if(slot == badSkill)
            {
                if (playerFailed[playerNum])
                    return;
                UnityEngine.Debug.Log($"Player {playerNum} failed BadSkill");
                playerFailed[playerNum] = true;
                numPlayersFailed++;
                UpdateProgress();
                // have all but one player failed?
                if (numPlayersFailed >= totalNumberPlayers-1)
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

        void Reset()
        {
            if (playerFailed is null)
                return;
            for (int i = 0; i < playerFailed.Length; i++)
            {
                playerFailed[i] = false;
            }
            ResetProgress();
        }
    }
}
