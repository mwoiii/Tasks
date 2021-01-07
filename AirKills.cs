using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tasks
{
    class AirKills : Task
    {
        int kills = 0;
        int killsNeeded = 3;

        public override void Init()
        {
            base.Init();
            GlobalEventManager.onCharacterDeathGlobal += OnKill;
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            GlobalEventManager.onCharacterDeathGlobal -= OnKill;

        }

        protected override bool IsComplete()
        {
            return kills >= killsNeeded;
        }

        public void OnKill(DamageReport damageReport)
        {
            if(Airborne())
            {
                kills++;
                if(IsComplete())
                {
                    EndTask();
                }
            }
        }

        bool Airborne()
        {
            return false;
        }

        public void OnLanding()
        {
            kills = 0;
        }
    }
}
