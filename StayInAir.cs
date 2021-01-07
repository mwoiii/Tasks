using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tasks
{
    class StayInAir : Task
    {
        float timeInAir;
        float timeToStayInAir = 10;

        override protected bool IsComplete()
        {
            return timeInAir >= timeToStayInAir;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if(Airborne())
            {
                timeInAir += Time.fixedDeltaTime;
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
            timeInAir = 0;
        }
    }
}
