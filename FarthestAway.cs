using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;

namespace Tasks
{
    class FarthestAway : Task
    {
        protected new string description { get; } = "Farthest away in 20s wins";

        public override TaskType type { get; } = TaskType.FarthestAway;
        protected override string name { get; } = "Farthest From Spawn";

        Vector3[] startPositions;
        bool active = false;

        IEnumerator timerRoutine;

        float winnerDist = 0;

        public override bool CanActivate(int numPlayers)
        {
            return numPlayers > 1;
        }

        public override string GetDescription()
        {
            return description;
        }

        public override string GetWinMessage(int winningPlayer)
        {
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by being {GetStylizedTaskWinStat(winnerDist.ToString("F2"))}m away in 20 seconds.";
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set Hooks in FarthestAway. {numPlayers} players");

            base.SetHooks(numPlayers);


            if (startPositions is null || startPositions.Length != numPlayers)
            {
                startPositions = new Vector3[numPlayers];
            }

            for (int i = 0; i < startPositions.Length; i++)
            {
                // probably broken if one player DCs
                startPositions[i] = TasksPlugin.GetPlayerCharacterMaster(i).GetBody().transform.position;

                // are they up in the air?
                // doesn't seem to be. 
                // FarthestAway(0): (13.3, 4.0, 33.1) -> (15.1, 4.0, -12.0) = 45.13267.  titan plains
                // FarthestAway(0): (-4.8, -149.2, 97.0) -> (203.1, -133.6, -71.4) = 268.0448 swamp
                // FarthestAway(0): (229.0, 30.2, -64.3) -> (44.0, 3.8, -34.2) = 189.251. snow map
            }

            timerRoutine = EndTask();
            TasksPlugin.instance.StartCoroutine(timerRoutine);
            active = true;
        }

        protected override void Unhook()
        {
            if (!active)
                return;
            active = false; // can probably delete this. Shouldn't matter if stopCo runs twice
            // but I don't know what happens if unhook runs before setHooks
            // i.e. what if timerRoutine == null?
            // maybe this is safe enough?
            if (timerRoutine != null)
            {
                TasksPlugin.instance.StopCoroutine(timerRoutine);
            }

            base.Unhook();
        }

        void UpdateProgress(int time)
        {
            // I could have this just be the timer. So it just fills up over the 20s
            // or I could have that AND show the relative difference between the players.
            // but then I would have to calculate distance every second. Which isn't that big of a deal
            // I think the task would feel better with the progress to see if you're winning or it's close, etc.
            // instead of two players having different intensity. One is super try hard bc he thinks the other is right on his heels and everyone else is not trying
            float[] currentDist = new float[startPositions.Length];
            float maxDist = 0;
            
            if (time > 0)
            {
                for (int i = 0; i < startPositions.Length; i++)
                {
                    currentDist[i] = Vector3.Distance(startPositions[i], TasksPlugin.GetPlayerCharacterMaster(i).GetBody().transform.position);
                    if (currentDist[i] > maxDist)
                    {
                        maxDist = currentDist[i];
                    }
                }
            }
            if (maxDist > 0)
            {
                for (int i = 0; i < progress.Length; i++)
                {
                    progress[i] = (time * (currentDist[i] / maxDist)) / 20;
                }
            }
            else
            {
                for (int i = 0; i < progress.Length; i++)
                {
                    // if time is 0, it skips the distance calc so maxDist is 0. Reset the progress bar
                    progress[i] = 0;
                }
            }
            base.UpdateProgress(progress);
        }

        IEnumerator EndTask()
        {
            for (int i = 0; i < 20; i++)
            {
                yield return new WaitForSeconds(1);
                UpdateProgress(i+1);
            }
            //yield return new WaitForSeconds(20);
            Evaluate();
        }

        void Evaluate()
        {
            float mostDist = 0;
            int winner = 0;


            for (int i = 0; i < startPositions.Length; i++)
            {
                // skip players who are dead
                CharacterMaster charMast = TasksPlugin.GetPlayerCharacterMaster(i);
                if (charMast == null) continue;
                CharacterBody charBody = charMast.GetBody();
                if (charBody == null) continue;

                Vector3 endPos = charBody.transform.position;
                float dist = Vector3.Distance(startPositions[i], endPos);

                if (dist > mostDist)
                {
                    mostDist = dist;
                    winner = i;
                }
                //Chat.AddMessage($"FarthestAway({i}): {startPositions[i]} -> {endPos} = {dist}. Winner: {winner} with {mostDist}");
            }
            winnerDist = mostDist;
            CompleteTask(winner);
            ResetProgress();
        }

    }
}
