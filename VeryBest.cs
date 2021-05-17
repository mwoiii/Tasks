using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;

namespace Tasks
{
    class VeryBest : Task
    {
        // Be the very best. Like no one ever was
        public override TaskType type { get; } = TaskType.VeryBest;

        HashSet<string>[] mobNames;
        bool active = false;

        public override bool CanActivate(int numPlayers)
        {
            return numPlayers > 1;
        }

        public override string GetDescription()
        {
            return "Be the very best. Like no one ever was.";
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set hooks in VeryBest. {numPlayers} players");
            base.SetHooks(numPlayers);

            GlobalEventManager.onCharacterDeathGlobal += OnKill;

            mobNames = new HashSet<string>[numPlayers];
            for (int i = 0; i < mobNames.Length; i++)
            {
                mobNames[i] = new HashSet<string>();
            }

            ResetProgress();
            active = true;
        }

        protected override void Unhook()
        {
            if (!active)
                return;
            active = false;

            Evaluate();

            GlobalEventManager.onCharacterDeathGlobal -= OnKill;

            Reset();

            base.Unhook();
        }

        public void OnKill(DamageReport damageReport)
        {
            if (damageReport is null) return;
            if (damageReport.attackerMaster is null) return;
            if (damageReport.attackerMaster.playerCharacterMasterController is null) return;

            int playerNum = TasksPlugin.GetPlayerNumber(damageReport.attackerMaster);

            // Player 0 won with 4 different mobs. Beetle, Lesser Wisp, Lemurian, Stone Titan
            // I swear it didn't count my stone golem kills. Were they all killed by AtGs?
            // the achievement for killing an elite doesn't do any special checks to see if an AtG kills it
            // Does that mean AtG kills still count for you or they didn't care?
            // I'm 100% sure my firework rocket kills counted
            // Player 0 won with 9 different mobs. Beetle, Lesser Wisp, Lemurian, Stone Golem, Overloading Beetle, Beetle Guard, , Beetle Queen, Blazing Lesser Wisp
            // Tried again and it seemed to record all the mobs. Blank one is beetle queen spawn things (what do they even do?)

            // returns beetle or Glacial beetle, etc. which is what I want
            string name = Util.GetBestBodyName(damageReport.victimBody.gameObject);
            //Chat.AddMessage($"Player {playerNum} killed a {name}");
            
            if (mobNames[playerNum].Contains(name))
                return;
            mobNames[playerNum].Add(name);
            UpdateProgress();
        }

        void UpdateProgress()
        {
            int bestCount = 0;
            for (int i = 0; i < mobNames.Length; i++)
            {
                int count = mobNames[i].Count;
                if (count > bestCount)
                {
                    bestCount = count;
                }
            }
            if (bestCount > 0)
            {
                for (int i = 0; i < progress.Length; i++)
                {
                    progress[i] = ((float)mobNames[i].Count / (float)bestCount);
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
            if (mobNames is null)
                return;
            int bestPlayer = 0;
            int bestCount = 0;
            for (int i = 0; i < mobNames.Length; i++)
            {
                int count = mobNames[i].Count;
                if(count > bestCount)
                {
                    bestPlayer = i;
                    bestCount = count;
                }
            }

            Debug.Log($"Player {bestPlayer} won with {bestCount} different mobs. {string.Join(", ", mobNames[bestPlayer])}");
            CompleteTask(bestPlayer);
        }

        void Reset()
        {
            if (mobNames is null)
                return;

            for (int i = 0; i < mobNames.Length; i++)
            {
                mobNames[i].Clear();
            }
            ResetProgress();
        }
    }
}
