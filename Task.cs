using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace Tasks
{
    public enum TaskType { Base, AirKills, DamageMultiple, DamageInTime, StayInAir, BiggestHit, MostDistance, PreonEvent, FarthestAway, FailShrine, OpenChests, StartTele, UsePrinters, OrderedSkills, BadSkill, BabyDrone, Die, FindLockbox, HealingItem, NoJump, VeryBest, FewestElites, GetLucky, GetLow, KillStreak, QuickDraw, FarKill };

    // Put plugin here
    // C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\BepInEx\plugins\MyMods

    /*
    [R2APISubmoduleDependency(nameof(UnlockablesAPI))]
    // Adds Task : ModdedUnlockableAndAchievements
    class Task : ModdedUnlockableAndAchievement<CustomSpriteProvider>
    */
    class Task // : ModdedUnlockableAndAchievement<VanillaSpriteProvider>
    {
        static string myName = "SOLRUN_";
        static string myMod = "TASKS_";
        protected static string thisClass = "BASE_TASK_";
        protected string description { get; } = "Base description";

        public virtual TaskType type { get; } = TaskType.Base;
        protected virtual string name { get; } = "Base Task";
        public static event Action<TaskType, int, string> OnCompletion;
        public static event Action<TaskType, float[]> OnUpdateProgress;

        //UserProfile profile;
        //protected UserAchievementManager ownerCached;
        protected int totalNumberPlayers;
        protected float[] progress;

        bool taskActive = false;

        public Task()
        {
            OnInstall();
        }

        virtual protected void SetupName()
        {
            //Language.currentLanguage.SetStringByToken(AchievementNameToken, name);
        }

        public virtual string GetDescription()
        {
            return description;
        }

        virtual public void OnInstall()
        {
            //base.OnInstall();
            //SetupName();
            // cache the profile
            // need it for showing the popup, revoking achievements
            //profile = this.owner.userProfile;
            //ownerCached = this.owner;

            // Should I call this here?
            // The problem is if you start the game with a task achievement completed in your log, it won't track anymore (bc achievements are one-off things)
            // So if you start the game, will even this part get run?
            // It doesn't help
            //RemoveAchievement();

            // Not sure if this is the right event
            // Other options
            // These didn't seem to ever trigger for me
            //Run.onServerGameOver
            //Run.onClientGameOverGlobal
            // Need it to remove the achievement if you complete a task
            // and then quit the game before activating it again (bc activating clears it first)
            Run.onRunDestroyGlobal += RunOver;
            
            TasksPlugin.OnActivate += this.Activate;
            TasksPlugin.OnDeactivate += this.Deactivate;
            TasksPlugin.OnCancelAll += this.CancelAllTasks;
        }

        virtual public void OnUninstall()
        {
            // I'm not sure when it's appropriate to call this method
            // I don't think I ever need to
            // These hooks are only set when you launch RoR2
            // so they shouldn't get set twice
            // so this would get called when you quit to desktop
            // so it doesn't really need to be called at all

            // Do I want these here?
            Run.onRunDestroyGlobal -= RunOver;

            TasksPlugin.OnActivate -= this.Activate;
            TasksPlugin.OnDeactivate -= this.Deactivate;
            TasksPlugin.OnCancelAll -= this.CancelAllTasks;

            // Gets called automatically when I set granted to true
            // but I don't do that because
            // setting granted to true causes the achievement to be deleted
            // which I don't want bc I want to reactivate and reuse them
            //base.OnUninstall();
        }

        virtual protected void UpdateProgress(float[] progress)
        {
            if(taskActive)
                OnUpdateProgress?.Invoke(type, progress);
        }

        protected void ResetProgress()
        {
            /*
             * When quiting to desktop from in game. Dunno if this matters
             [Error  : Unity Log] NullReferenceException
             Stack trace:
             UnityEngine.MonoBehaviour.StartCoroutine (System.Collections.IEnumerator routine) (at <2cc17dca390941eeb4d7b2ff1f84696a>:IL_0012)
             Tasks.Task.ResetProgress () (at <9e2eee2d661
             */

            /*
            [Error  : Unity Log] NullReferenceException
            Stack trace:
            UnityEngine.MonoBehaviour.StartCoroutine (System.Collections.IEnumerator routine) (at <2cc17dca390941eeb4d7b2ff1f84696a>:IL_0012)
            Tasks.Task.ResetProgress () (at <e1ba81fdd7304fd0b708ef751fe456c8>:IL_0012)
            Tasks.DamageMultipleTargets.ResetKills () (at <e1ba81fdd7304fd0b708ef751fe456c8>:IL_0037)
            Tasks.DamageMultipleTargets.Unhook () (at <e1ba81fdd7304fd0b708ef751fe456c8>:IL_0025)
            Tasks.Task.RunOver (RoR2.Run run) (at <e1ba81fdd7304fd0b708ef751fe456c8>:IL_0001)
            RoR2.Run.OnDestroy () (at <da7c19fa62814b28bdb8f3a9223868e1>:IL_0009) 

            When quitting to desktop in a run
            Is the problem with starting the coroutine or ResetProgressInTime?
             */
            TasksPlugin.instance?.StartCoroutine(ResetProgressInTime());
        }

        IEnumerator ResetProgressInTime()
        {
            // you get 3 secs of a full bar
            yield return new WaitForSeconds(4);
            if (progress != null)
            {
                for (int i = 0; i < progress.Length; i++)
                {
                    progress[i] = 0;
                }
                // has the task ended? like when you quit the game?
                if (taskActive)
                {
                    OnUpdateProgress?.Invoke(type, progress);
                }
                else
                {
                    // I couldn't get this to run. Maybe this wasn't the issue?
                    // get null reference exception and this message DOESN'T appear
                    // so it doesn't stop that from happening
                    // maybe this message plays in between stages? So it might still be neeeded
                    Debug.Log($"{type:g} was inactive. Don't update progress");
                }
            }
        }

        public virtual string GetWinMessage(int winningPlayer)
        {
            return $"{GetStylizedName(winningPlayer)} completed {name}";
        }

        protected string GetStylizedName(int playerNum)
        {
            return $"<color=#ffffff>{TasksPlugin.GetPlayerName(playerNum)}</color>";
        }

        protected string GetStylizedTaskName(string taskName)
        {
            return $"<style=cIsUtility>{taskName}</style>"; // utility is light blue
        }

        protected string GetStylizedTaskWinStat(string stat)
        {
            // whatever the task tracks like kills or time
            return $"<style=cIsUtility>{stat}</style>";
        }

        virtual protected void CompleteTask(int playerNum)
        {
            //ShowPopup();
            Unhook();
            // send message to server
            // id is this task
            // netId is the player
            OnCompletion?.Invoke(type, playerNum, GetWinMessage(playerNum));
        }

        virtual protected bool IsComplete(int playerNum)
        {
            return false;
        }

        public virtual bool CanActivate(int numPlayers)
        {
            return true;
        }

        void Activate(int id, int numPlayers)
        {
            //Chat.AddMessage($"{id} == {(int)type}: {(id == (int)type)}");
            if(id == (int)type)
            {
                totalNumberPlayers = numPlayers;
                progress = new float[numPlayers];
                SetHooks(numPlayers);
                taskActive = true;
            }
        }

        public void Deactivate(int id)
        {
            if(id == (int)type || id < 0)
            {
                taskActive = false;
                StageEnd();
                Unhook();
            }
        }

        public void CancelAllTasks()
        {
            taskActive = false;
            Unhook();
        }

        protected virtual void StageEnd()
        {

        }

        protected virtual void SetHooks(int numPlayers)
        {
            // Setup achievement-specific hooks
            // Like:
            // GlobalEventManager.onCharacterDeathGlobal += this.CheckDeath;
            // and then create a class called CheckDeath()

        }

        protected virtual void Unhook()
        {
            // -= whatever you hooked in setHooks
            // or else next time you activate this task, those hooks will get called twice
            // this gets called by tasks that don't have an 'active' field (some don't need it) when you leave the game
            // and it gets called when you complete the task
            //Debug.Log($"Unhooked {type:g}");
        }

        void RunOver(Run run)
        {
            // runs when you get to the stats screen and press the button to continue
            // and when you quit to main menu from the pause menu
            //Debug.Log($"Task.RunOver {name}");
            Unhook();

            OnUninstall();
            
        }
    }
}
