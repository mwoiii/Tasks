using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tasks
{
    class StayInAir : Task
    {
        protected new string description { get; } = "Stay airborne for 10 seconds";
        /*
        public override string AchievementIdentifier { get; } = "SOLRUN_TASKS_STAY_AIRBORNE_ACHIEVEMENT_ID"; // delete this from XML if there
        public override string UnlockableIdentifier { get; } = "SOLRUN_TASKS_STAY_AIRBORNE_REWARD_ID"; // Delete me from XML too
        // I think all this does is hide it in the log until you have the prereq. You could still complete it (except most prereqs seem to be characters)
        public override string PrerequisiteUnlockableIdentifier { get; } = "";
        public override string AchievementNameToken { get; } = "SOLRUN_TASKS_STAY_AIRBORNE_ACHIEVEMENT_NAME"; // Fine to have in the XML
        public override string AchievementDescToken { get; } = description; // plain English
        public override string UnlockableNameToken { get; } = ""; // plain English
        */
        public override TaskType type { get; } = TaskType.StayInAir;
        protected override string name { get; } = "Stay Airborne";

        CharacterMotor[] motors;
        CharacterBody[] bodies;

        float[] timeInAir;
        float timeToStayInAir = 5;

        public override string GetDescription()
        {
            return $"Stay in the air for {Math.Round(timeToStayInAir, 1)} seconds";
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set Hooks in Stay Airborne. {numPlayers} players");

            base.SetHooks(numPlayers);

            timeToStayInAir = 3 + Run.instance.difficultyCoefficient;

            if (timeInAir is null || timeInAir.Length != numPlayers)
            {
                timeInAir = new float[numPlayers];
                for (int i = 0; i < timeInAir.Length; i++)
                {
                    timeInAir[i] = 0;
                }
            }

            if(motors is null || motors.Length != numPlayers)
            {
                motors = new CharacterMotor[numPlayers];
            }

            if(bodies is null || motors.Length != numPlayers)
            {
                bodies = new CharacterBody[numPlayers];
            }

            //Reset();
            SetupBodies();
            
            // This is how the merc's stay in the air achieve works
            RoR2Application.onFixedUpdate += AirborneFixedUpdate;
        }

        protected override void Unhook()
        {
            RoR2Application.onFixedUpdate -= AirborneFixedUpdate;

            Reset();

            base.Unhook();
        }

        protected void UpdateProgress()
        {
            if (timeInAir is null || progress is null)
                return;
            for (int i = 0; i < progress.Length; i++)
            {
                progress[i] = timeInAir[i] / timeToStayInAir;
            }
            base.UpdateProgress(progress);
        }

        private void AirborneFixedUpdate()
        {
            UpdateProgress(); // never gets to 1.0

            // does this break when one player dies?
            for (int i = 0; i < timeInAir.Length; i++)
            {
                timeInAir[i] = ((motors[i] && !motors[i].isGrounded && !bodies[i].currentVehicle) ? (timeInAir[i] + Time.fixedDeltaTime) : 0f);
                if(IsComplete(i))
                {
                    //Chat.AddMessage($"Player {i} Completed StayAirborne");
                    CompleteTask(i);
                    Reset();
                }
            }
        }

        override protected bool IsComplete(int playerNum)
        {
            return timeInAir[playerNum] >= timeToStayInAir;
        }

        void Reset()
        {
            UpdateProgress(); // update before you reset so the bar is full for a bit
            if (timeInAir is null)
                return;
            for (int i = 0; i < timeInAir.Length; i++)
            {
                timeInAir[i] = 0;
            }
            ResetProgress();
        }

        void SetupBodies()
        {
            for (int i = 0; i < motors.Length; i++)
            {
                CharacterMaster current = TasksPlugin.GetPlayerCharacterMaster(i);
                bodies[i] = current.GetBody();
                motors[i] = current.GetBody().characterMotor;
            }
        }
    }
}
