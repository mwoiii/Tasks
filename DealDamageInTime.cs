using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tasks
{
    class DealDamageInTime : Task
    {
        float damageToDeal = 5000;
        float timeLimit = 5;

        float currentDamage;

        public void OnDamage(float damage)
        {
            currentDamage += damage;
            if(IsComplete(0))
            {
                CompleteTask();
            }
        }

        IEnumerator ReduceDamage(float damage)
        {
            yield return new WaitForSeconds(timeLimit);
            currentDamage -= damage;
        }
    }
}
