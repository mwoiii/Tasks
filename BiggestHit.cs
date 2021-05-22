using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tasks
{
    class BiggestHit : Task
    {
        protected new string description { get; } = "Biggest hit wins";

        public override TaskType type { get; } = TaskType.BiggestHit;
        protected override string name { get; } = "Biggest Hit";


        // This task doesn't actually make much sense. Most abilitites do a set amount of damage.
        // Whomever has the biggest ability wins every time
        // would need to be biggest ability combined with all of the effects like ukulele and bands, etc
        // Most damage within 0.2s??

        float[] biggestHit;
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
            Debug.Log($"Set Hooks in BiggestHit. {numPlayers} players");

            base.SetHooks(numPlayers);

            GlobalEventManager.onServerDamageDealt += OnDamage;
            
            //TeleporterInteraction.onTeleporterFinishGlobal += Evaluate;

            if (biggestHit is null || biggestHit.Length != numPlayers)
            {
                biggestHit = new float[numPlayers];
            }
            Reset();
            active = true;
        }

        protected override void Unhook()
        {
            if (!active)
                return;
            //Chat.AddMessage("Unhook BiggestHit. This should only run once");
            active = false;
            
            Evaluate();

            GlobalEventManager.onServerDamageDealt -= OnDamage;

            Reset();

            base.Unhook();
        }

        protected void UpdateProgress()
        {
            float bigHit = 0;
            for (int i = 0; i < biggestHit.Length; i++)
            {
                if (biggestHit[i] > bigHit)
                {
                    bigHit = biggestHit[i];
                }
            }
            if (bigHit > 0)
            {
                for (int i = 0; i < progress.Length; i++)
                {
                    progress[i] = biggestHit[i] / bigHit;
                }
            }
            else
            {
                for (int i = 0; i < progress.Length; i++)
                {
                    progress[i] = 0; // shouldn't ever need this
                }
            }
            base.UpdateProgress(progress);
        }

        public void OnDamage(DamageReport report)
        {
            if (report is null) return;
            if (report.attackerMaster is null) return;
            if (report.attackerMaster.playerCharacterMasterController is null) return;

            // Did I hit it?
            int playerNum = TasksPlugin.GetPlayerNumber(report.attackerMaster);
            
            float damage = report.damageDealt;
            if(damage > biggestHit[playerNum])
            {
                //Debug.Log($"Set new big hit for player {playerNum}. {biggestHit[playerNum]} -> {damage}");
                biggestHit[playerNum] = damage;
                UpdateProgress();
            }
        }

        void Evaluate()
        {
            float biggest = 0;
            int bigPlayer = 0;

            for (int i = 0; i < biggestHit.Length; i++)
            {
                if(biggestHit[i] > biggest)
                {
                    biggest = biggestHit[i];
                    bigPlayer = i;
                }
            }

            CompleteTask(bigPlayer);
        }

        void Reset()
        {
            if (biggestHit is null)
                return;

            for (int i = 0; i < biggestHit.Length; i++)
            {
                biggestHit[i] = 0;
            }
            ResetProgress();
        }
    }
}