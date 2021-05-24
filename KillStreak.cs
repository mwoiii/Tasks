using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;

namespace Tasks
{
    class KillStreak : Task
    {
        public override TaskType type { get; } = TaskType.KillStreak;

        int[] bestStreaks;
        int[] currentStreaks;
        bool active = false;

        public override bool CanActivate(int numPlayers)
        {
            return true;// numPlayers > 1;
        }

        public override string GetDescription()
        {
            return "Most kills without getting hit"; // without taking damage? Depends if fall damage ends it
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set Hooks in KillStreak. {numPlayers} players");

            base.SetHooks(numPlayers);

            GlobalEventManager.onServerDamageDealt += OnDamage;
            GlobalEventManager.onCharacterDeathGlobal += OnKill;

            bestStreaks = new int[numPlayers];
            currentStreaks = new int[numPlayers];
            Reset();
        }

        protected override void Unhook()
        {
            if (!active)
                return;
            active = false;

            Evaluate();

            GlobalEventManager.onServerDamageDealt -= OnDamage;
            GlobalEventManager.onCharacterDeathGlobal -= OnKill;

            Reset();

            base.Unhook();
        }

        void OnDamage(DamageReport report)
        {
            if (report.victimMaster.playerCharacterMasterController is null) return;

            // any damage ends your streak
            // TODO: ignore fall damage?
            // TODO: ignore self damage (REX). does vicitm == attacker work?
            // you got hit
            //Debug.Log($"KillStreak. Fall damage? {report.isFallDamage}");
            
            // self damage
            // victim == attacker doesn't work
            if (report.victimMaster == report.attackerMaster)
            {
                // fall damage isn't self damage
                // REX damage also isn't self damage. Dunno how to figure that out. friendly fire?
                Debug.Log($"KillStreak. Self damage ignore."); 
                return;
            }
            int playerNum = TasksPlugin.GetPlayerNumber(report.victimMaster);

            if (playerNum > -1)
            {
                //UpdateStreaks(playerNum);
                Debug.Log($"Hit. Resetting streak for P{playerNum}");
                currentStreaks[playerNum] = 0;
            }
        }

        void OnKill(DamageReport report)
        {
            if (report.attackerMaster.playerCharacterMasterController is null) return;
            int playerNum = TasksPlugin.GetPlayerNumber(report.attackerMaster);

            if (playerNum > -1) // whoops. was > 0 so player 0 could never win lol
            {
                currentStreaks[playerNum]++;
                UpdateStreaks(playerNum);
            }
        }

        void UpdateStreaks(int playerNum)
        {
            if (currentStreaks[playerNum] > bestStreaks[playerNum])
            {
                Debug.Log($"Player {playerNum} improved their streak. {bestStreaks[playerNum]} -> {currentStreaks[playerNum]}");
                bestStreaks[playerNum] = currentStreaks[playerNum];
                UpdateProgress();
            }
        }

        protected void UpdateProgress()
        {
            // this progress might not be the best
            // it shows your best streak, not your current streak
            // so you can't see your current streak unless it's your best.
            // might look like it's broken
            // would need to have custom ui for this task to fix that
            // could use blue for leader
            // green for your best
            // gold for current
            // if you're winning, won't be able to see how close your rival is
            int best = 0;
            for (int i = 0; i < bestStreaks.Length; i++)
            {
                if(bestStreaks[i] > best)
                {
                    best = bestStreaks[i];
                }
            }
            if(best > 0)
            {
                for (int i = 0; i < progress.Length; i++)
                {
                    progress[i] = (float)bestStreaks[i] / (float)best;
                }
            }
            else
            {
                for (int i = 0; i < progress.Length; i++)
                {
                    progress[i] = 0;
                }
            }
            base.UpdateProgress(progress);
        }

        void Evaluate()
        {
            int winner = 0;
            int best = 0;

            for (int i = 0; i < bestStreaks.Length; i++)
            {
                if(bestStreaks[i] > best)
                {
                    best = bestStreaks[i];
                    winner = i;
                }
            }
            Debug.Log($"Player {winner} won with a streak of {best}");
            CompleteTask(winner);
        }

        void Reset()
        {
            if (bestStreaks is null || currentStreaks is null)
                return;
            for (int i = 0; i < bestStreaks.Length; i++)
            {
                bestStreaks[i] = 0;
                currentStreaks[i] = 0;
            }
            ResetProgress();
        }
    }
}
