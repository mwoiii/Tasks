using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;


namespace Tasks
{
    class NoJump : Task
    {
        public override TaskType type { get; } = TaskType.NoJump;

        bool[] playerFailed;
        int numPlayersFailed;

        public override string GetDescription()
        {
            return "Don't jump";
        }

        public override string GetWinMessage(int winningPlayer)
        {
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by being the last to jump.";
        }

        public override bool CanActivate(int numPlayers)
        {
            return numPlayers>1;
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set Hooks in NoJump. {numPlayers} players");
            base.SetHooks(numPlayers);

            playerFailed = new bool[numPlayers];
            Reset();

            // don't seem to exist
            //On.RoR2.CharacterMotor.Jump +=
            //IL.RoR2.CharacterMotor.Jump
            // I think this is called when you jump. It's possible it's also called when hitting a jump pad
            On.EntityStates.GenericCharacterMain.ApplyJumpVelocity += OnJump;
        }

        protected override void StageEnd()
        {
            base.StageEnd();

            Evaluate();
            Reset();
        }

        protected override void Unhook()
        {
            On.EntityStates.GenericCharacterMain.ApplyJumpVelocity -= OnJump;

            base.Unhook();
        }

        void Evaluate()
        {
            // reward anyone left
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
                progress[i] = playerFailed[i] ? 0 : (numPlayersFailed / (totalNumberPlayers - 1));
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

        void OnJump(On.EntityStates.GenericCharacterMain.orig_ApplyJumpVelocity orig, CharacterMotor characterMotor, CharacterBody characterBody, float horizontalBonus, float verticalBonus, bool vault = false)
        {
            // do the real version
            // can this go first or does it need to go last
            orig(characterMotor, characterBody, horizontalBonus, verticalBonus, vault);


            // someone jumped
            int playerNum = TasksPlugin.GetPlayerNumber(characterBody.master);
            if (playerNum < 0)
                return;

            if (playerFailed[playerNum])
                return; // already failed
            Debug.Log($"Player {playerNum} failed NoJump");
            // Fancy chat message here
            // R2API.Utils.ChatMessage.Send() maybe?
            playerFailed[playerNum] = true;
            numPlayersFailed++;
            UpdateProgress();

            // only 1 left?
            if(numPlayersFailed >= totalNumberPlayers-1)
            {
                for (int i = 0; i < playerFailed.Length; i++)
                {
                    if(!playerFailed[i])
                    {
                        CompleteTask(i);
                        return;
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
