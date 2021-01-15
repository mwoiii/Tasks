using System;
using System.Collections.Generic;
using System.Text;
using R2API;
using R2API.Utils;
using RoR2;

namespace Tasks
{
    // Put plugin here
    // C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2\BepInEx\plugins\MyMods

    /*
    [R2APISubmoduleDependency(nameof(UnlockablesAPI))]
    // Adds Task : ModdedUnlockableAndAchievements
    class Task : ModdedUnlockableAndAchievement<CustomSpriteProvider>
    */
    class Task : ModdedUnlockableAndAchievement<VanillaSpriteProvider>
    {
        static string myName = "SOLRUN_";
        static string myMod = "TASKS_";
        protected static string thisClass = "BASE_TASK_";
        public override string AchievementIdentifier { get; } = myName + myMod + thisClass + "ACHIEVEMENT_ID";
        public override string UnlockableIdentifier { get; } = myName + myMod + thisClass + "REWARD_ID";
        // I think all this does is hide it in the log until you have the prereq. You could still complete it (except most prereqs seem to be characters)
        public override string PrerequisiteUnlockableIdentifier { get; } = myName + myMod + thisClass + "PREREQUISITE_ID";
        public override string AchievementNameToken { get; } = myName + myMod + thisClass + "ACHIEVEMENT_NAME"; // plain English
        public override string AchievementDescToken { get; } = myName + myMod + thisClass + "ACHIEVEMENT_DESC"; // plain English
        public override string UnlockableNameToken { get; } = myName + myMod + thisClass + "UNLOCKABLE_NAME"; // plain English
        protected override VanillaSpriteProvider SpriteProvider { get; } = new VanillaSpriteProvider("VANILLA PATH");

        //protected virtual int _id { get;} = 0;
        protected virtual TaskType type { get; } = TaskType.Base;
        protected virtual string name { get; } = "Base Task";
        public static event Action<TaskType, int> OnCompletion;

        UserProfile profile;
        protected UserAchievementManager ownerCached;
        protected int totalNumberPlayers;

        virtual protected void SetupName()
        {
            Language.currentLanguage.SetStringByToken(AchievementNameToken, name);
        }

        override public void OnInstall()
        {
            base.OnInstall();
            SetupName();
            // cache the profile
            // need it for showing the popup, revoking achievements
            profile = this.owner.userProfile;
            ownerCached = this.owner;

            // Not sure if this is the right event
            // Other options
            //Run.onServerGameOver
            //Run.onClientGameOverGlobal
            // Need it to remove the achievement if you complete a task
            // and then quit the game before activating it again (bc activating clears it first)
            Run.onRunDestroyGlobal += RunOver;

            TasksPlugin.OnActivate += this.Activate;
            TasksPlugin.OnDeactivate += this.Deactivate;
            TasksPlugin.OnResetAll += this.RemoveAchievement;
            TasksPlugin.OnPopup += this.ShowPopup;
        }

        override public void OnUninstall()
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
            TasksPlugin.OnResetAll -= this.RemoveAchievement;
            TasksPlugin.OnPopup -= this.ShowPopup;


            // Gets called automatically when I set granted to true
            // but I don't do that because
            // setting granted to true causes the achievement to be deleted
            // which I don't want bc I want to reactivate and reuse them
            base.OnUninstall();
        }

        virtual protected void CompleteTask()
        {
            // suppress errors for now
        }
        virtual protected void CompleteTask(int playerNum)
        {
            //ShowPopup();
            Unhook();
            // send message to server
            // id is this task
            // netId is the player
            OnCompletion?.Invoke(type, playerNum);
        }

        virtual protected bool IsComplete(int playerNum)
        {
            return false;
        }

        void ShowPopup()
        {
            // how to make this run on the client?
            //CharacterMaster m;
            //TasksPlugin.GetPlayerCharacterMaster(playerNum).GetComponent<UserProfile>().AddAchievement(this.achievementDef.identifier, false);
            profile.AddAchievement(this.achievementDef.identifier, false);
        }

        void ShowPopup(TaskType t)
        {
            if(t == type)
            {
                ShowPopup();
            }
        }

        void RemoveAchievement()
        {
            if (profile != null)
            {
                if (profile.HasAchievement(AchievementIdentifier))
                {
                    profile.RevokeAchievement(AchievementIdentifier);
                }
                profile.RevokeUnlockable(UnlockableCatalog.GetUnlockableDef(UnlockableIdentifier));
            }
        }

        void Activate(int id, int numPlayers)
        {
            //Chat.AddMessage($"{id} == {(int)type}: {(id == (int)type)}");
            if(id == (int)type)
            {
                //Chat.AddMessage("Activated " + id);
                // Need to remove once for each time you get the achievement
                // and Can't do it the same frame you get the achieve
                // so doing it before you activate again works
                RemoveAchievement();
                totalNumberPlayers = numPlayers;
                SetHooks(numPlayers);
            }
        }

        public void Deactivate(int id)
        {
            if(id == (int)type)
            {
                Unhook();
            }
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
        }

        void RunOver(Run run)
        {
            // Just in case
            RemoveAchievement();
        }
    }
}
