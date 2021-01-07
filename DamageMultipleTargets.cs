using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tasks
{
    class DamageMultipleTargets : Task
    {
        int numToHit = 3;
        HashSet<GameObject> targets;

        public override void Init()
        {
            base.Init();
            targets = new HashSet<GameObject>();

            // Can I do it this way somehow?
            // RoR.OnDamage += OnDamage;
            // RoR.OnKill += OnKill;
        }

        public void OnDamage(GameObject entity)
        {
            if(!targets.Contains(entity))
            {
                targets.Add(entity);
                // can't use game object. Need to know when it dies
                // entity.OnDeath += OnKill();
                if(IsComplete())
                {
                    EndTask();
                }
            }
        }

        protected override bool IsComplete()
        {
            return targets.Count >= numToHit;
        }

        void OnKill(GameObject entity)
        {
            targets.Remove(entity);
        }
    }
}
