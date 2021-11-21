using System;
using System.Collections.Generic;
using System.Text;
using RoR2;

namespace Tasks
{
    class Die : Task
    {
        public override TaskType type { get; } = TaskType.Die;

        public override bool CanActivate(int numPlayers)
        {
            return numPlayers > 1;
        }

        public override string GetDescription()
        {
            return "Die";
        }

        public override string GetWinMessage(int winningPlayer)
        {
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by dying first. Congratulations.";
        }

        protected override void SetHooks(int numPlayers)
        {
            base.SetHooks(numPlayers);

            GlobalEventManager.onCharacterDeathGlobal += OnKill;

        }

        protected override void Unhook()
        {
            GlobalEventManager.onCharacterDeathGlobal -= OnKill;

            base.Unhook();
        }

        public void OnKill(DamageReport damageReport)
        {
            if (damageReport is null) return;
            if (damageReport.victimMaster is null) return;

            int playerNum = TasksPlugin.GetPlayerNumber(damageReport.victimMaster);

            // -1 if not a player
            if(playerNum > -1)
            {
                CompleteTask(playerNum);
            }
        }

    }
}
