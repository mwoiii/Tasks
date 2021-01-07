using System;
using System.Collections.Generic;
using System.Text;
using R2API;
using R2API.Utils;
using RoR2;

namespace Tasks
{
    /*
    [R2APISubmoduleDependency(nameof(UnlockablesAPI))]
    // Adds Task : ModdedUnlockableAndAchievements
    class Task : ModdedUnlockableAndAchievement<CustomSpriteProvider>
    */
    class Task
    {
        virtual public void Init()
        {

        }

        virtual protected void Cleanup()
        {

        }

        virtual protected void EndTask()
        {
            // send message to server
        }

        virtual protected bool IsComplete()
        {
            return false;
        }

        virtual public void Update()
        {

        }
        virtual public void FixedUpdate()
        {

        }
    }
}
