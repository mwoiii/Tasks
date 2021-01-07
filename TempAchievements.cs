using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Achievements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tasks
{
    // Example from:
    // https://github.com/risk-of-thunder/R2API/wiki/UnlockablesAPI

    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin(GUID, MODNAME, VERSION)]
    [R2APISubmoduleDependency(nameof(UnlockablesAPI))]
    class TempAchievements : BaseUnityPlugin
    {
        public const string
            MODNAME = "Tasks",
            AUTHOR = "Solrun",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "0.0.0";


        public void Awake()
        {
            Chat.AddMessage("Loaded Beetle achievement");
            
            UnlockablesAPI.AddUnlockable<KillBeetle>(true);
            //KillBeetle beetle = new KillBeetle();
            //beetle.Revoke();
        }

    }


    public class KillBeetle : ModdedUnlockableAndAchievement<VanillaSpriteProvider>
    {
        static string myName = "SOLRUN_";
        static string myMod = "TEMP_ACHIEVEMENTS_";
        static string thisClass = "KILL_BEETLE_";
        public override string AchievementIdentifier { get; } = myName + myMod + thisClass + "ACHIEVEMENT_ID";
        public override string UnlockableIdentifier { get; } = myName + myMod + thisClass + "REWARD_ID";
        // I think all this does is hide it in the log until you have the prereq. You could still complete it (except most prereqs seem to be characters)
        public override string PrerequisiteUnlockableIdentifier { get; } = myName + myMod + thisClass + "PREREQUISITE_ID";
        public override string AchievementNameToken { get; } = myName + myMod + thisClass + "ACHIEVEMENT_NAME"; // plain English
        public override string AchievementDescToken { get; } = myName + myMod + thisClass + "ACHIEVEMENT_DESC"; // plain English
        public override string UnlockableNameToken { get; } = myName + myMod + thisClass + "UNLOCKABLE_NAME"; // plain English

        int _id = 0;
        protected override VanillaSpriteProvider SpriteProvider { get; } = new VanillaSpriteProvider("VANILLA PATH");

        UserProfile profile;
        UserAchievementManager ownerCache;

        public static event Action<int, uint> OnCompletion;


        public void CheckDeath(DamageReport report)
        {
            if (report is null) return;
            if (report.victimBody is null) return;

            // Not a very good way to check, but works for demonstration. The proper approach would involve getting the bodyindex and checking that.
            // this.superRoboBallBossBodyIndex = BodyCatalog.FindBodyIndex("SuperRoboBallBossBody");
            // could iterate through this to get them all probably
            //BodyCatalog.GetBodyName(0);
            if (report.victimBody.name.Contains("BeetleBody"))
            {
                // this can trigger twice on the same mob. bc Uninstall isn't being called automatically
                Chat.AddMessage("Got the beetle achievable");
                // Get this, then uninstall
                //base.Grant();
                ShowPopup();
                GlobalEventManager.onCharacterDeathGlobal -= this.CheckDeath;
                OnCompletion?.Invoke(_id, ownerCache.localUser.cachedMaster.netId.Value);

                // ID is 6 3/3 times I've ran the game. Starting the game, playing as commando. Quitting to desktop in between each time
                Chat.AddMessage("Net ID Value: " + ownerCache.localUser.cachedMaster.netId.Value.ToString());
                //Chat.AddMessage("Net ID: " + ownerCache.localUser.cachedMaster.netId.ToString());
                Chat.AddMessage("Profile name: " + profile.name); // This is probably the profile you input when you start RoR2 for the very first time
                //owner.userProfile.name
                

                //Chat.AddMessage("CharacterMaster: " + ownerCache.localUser.cachedMaster.);


                /*
                // These 3 seem fairly unique
                Chat.AddMessage("Net ID Value: " + ownerCache.localUser.cachedMaster.netId.Value.ToString());
                Chat.AddMessage("Net ID: " + ownerCache.localUser.cachedMaster.netId.ToString());
                Chat.AddMessage("Profile name: " + profile.name);

                Chat.AddMessage("Net ID Value: " + ownerCache.localUser.cachedMaster.playerControllerId);
                Chat.AddMessage("Name: " + ownerCache.localUser.cachedMaster.name);
                Chat.AddMessage("CharacterMaster: " + ownerCache.localUser.cachedMaster.ToString());
                Chat.AddMessage("Network Identity: " + ownerCache.localUser.cachedMaster.networkIdentity.ToString());
                */
                /*
                
                [Info   : Unity Log] Net ID Value: 6
                [Info   : Unity Log] Net ID: 6
                [Info   : Unity Log] Profile name: Solrun
                [Info: Unity Log] PlayerControllerId: -1 // this is to tell apart multiple players on one client. i.e. multiple controllers on a computer
                [Info: Unity Log] Name: CommandoMaster(Clone)
                [Info: Unity Log] CharacterMaster: CommandoMaster(Clone)(RoR2.CharacterMaster)
                [Info: Unity Log] Network Identity: CommandoMaster(Clone)(UnityEngine.Networking.NetworkIdentity)
                */
            }
        }

        public override void OnInstall()
        {

            // Gets called when you launch RoR
            // Not when you start a run
            // If you already have the achievement, it never gets called
            
            Chat.AddMessage("OnInstall");
            base.OnInstall();

            // cache the profile so I can revoke the achievement later
            profile = this.owner.userProfile;
            ownerCache = this.owner;

            //GlobalEventManager.onCharacterDeathGlobal += this.CheckDeath;

            // Should the -= be before the += or when the game ends?
            // which one is right?
            //Run.onClientGameOverGlobal += OnUninstallFinal;
            // this one seemed to work when I quit to menu
            // but not when I quit to desktop...
            // I wonder if it doesn't trigger if you end the game normally by dying or winning
            Run.onRunDestroyGlobal += GameOver;
            //Run.onServerGameOver
            OnUninstallFinal();
            // Add these so the manager can control when the achievement is available
            TasksPlugin.OnActivate += this.Activate;
            TasksPlugin.OnResetAll += this.Reset;
            TasksPlugin.OnPopup += this.ShowPopup;
            
        }

        public override void OnUninstall()
        {
            // this only gets called automatically the first time I get an achievement
            // even if I revoke that achievement
            Chat.AddMessage("Beetle Uninstall");
            
            // This gets called when you complete the achievement

            GlobalEventManager.onCharacterDeathGlobal -= this.CheckDeath;
            base.OnUninstall();
        }

        public void OnUninstallFinal()
        {
            // called when the game ends
            // I think you need this to be able to play multiple games without resetting the game
            TasksPlugin.OnActivate -= this.Activate;
            TasksPlugin.OnResetAll -= this.Reset;
            TasksPlugin.OnPopup -= this.ShowPopup;

        }

        void ShowPopup()
        {
            BaseAchievement baseAchievement = this;

            // this should add it to the queue of achievements to show
            // if true, I think it tries to get a steam popup
            profile.AddAchievement(baseAchievement.achievementDef.identifier, false);
            // Seems to work
            // Can't revoke immediately after because the popup doesn't happen until the next frame
            // All I get for output is
            // [Info   : Unity Log] Saved file "d5a8a646-5204-4408-a041-475a2506f979.xml" (142877 bytes)
            

            //profile.RevokeAchievement(baseAchievement.achievementDef.identifier);
            // This might be all I need to do the popup
            // Client is Facepunch.Steamworks
            // Is this for a steam achievement popup?
            //Client.Instance.Achievements.Trigger(baseAchievement.achievementDef.identifier, true);


            /*
            NetworkUser currentNetworkUser = ownerCache.localUser.currentNetworkUser;
            if (currentNetworkUser != null)
            {
                // maybe it can just use
                // AchievementNameToken
                currentNetworkUser.CallCmdReportAchievement(baseAchievement.achievementDef.nameToken);
            }
            */
            // What I get by running this method
            // this is yellow text in chat
            // [Info: Unity Log] < color =#ccd3e0>You achieved <color=#BDE151>SOLRUN_TEMP_ACHIEVEMENTS_KILL_BEETLE_ACHIEVEMENT_NAME</color></color>

            // What I get when I activate and then kill a beetle
            // That means the first few lines is from BaseAchievement.OnGranted()
            /*
            [Info: Unity Log] Got the beetle achievable
            [Info   : Unity Log] NetworkUser.CmdReportUnlock(RoR2.UnlockableIndex)
            [Info: Unity Log] NetworkUser.ServerHandleUnlock(SOLRUN_TEMP_ACHIEVEMENTS_KILL_BEETLE_REWARD_ID)
            [Info: Unity Log] Solrun unlocked SOLRUN_TEMP_ACHIEVEMENTS_KILL_BEETLE_UNLOCKABLE_NAME
            [Info: Unity Log] Beetle Uninstall
            [Info   : Unity Log] < color =#ccd3e0>You achieved <color=#BDE151>SOLRUN_TEMP_ACHIEVEMENTS_KILL_BEETLE_ACHIEVEMENT_NAME</color></color> 
            [Info: Unity Log] Saved file "d5a8a646-5204-4408-a041-475a2506f979.xml"(142940 bytes)
            */

            // On startup, the achievement list is populated (readonly)
            // When you call Granted, it sets toGrant = true;
            // In UserAchievementManager.Update, if toGrant is true,
            // it deletes the achievement from the list
            // and gives the achievement to you and gives you the popup, etc.

            // So I want to add my stuff to the list at the start (by inheriting from this API)
            // But I don't want to call Granted() because that leads to my achievements getting deleted
            // But I'm not sure what the minimum amount of stuff I have to call myself is
            // Do I need some of the server stuff?
            // unviewedAchievementList is private
            // Creating my own popup with DispatchAchievementNotifications(canvas, transform) is also private
        }

        void Activate(int id)
        {
            // The idea is that all tasks subscribe their activate to the manager
            // the manager picks random tasks by calling all of the activates and passing the random number
            // if the number matches, they activate
            // each task has a unique id
            Chat.AddMessage("Activate " + id + ". Want: " + _id);
            if(id == _id)
            {
                // need to remove once for each time its activated
                RemoveAchievement();
                // this is what really starts the achievement tracking
                // need to be careful about double calling activate
                GlobalEventManager.onCharacterDeathGlobal += this.CheckDeath;
                
            }
        }

        void Reset()
        {
            // userProfile becomes null when OnUninstall is called
            // so the userprofile needs to be cached

            // might be more stuff that needs to be reset

            this.shouldGrant = false;
            // this sets up the userProile again
            // kinda
            // base.OnUninstall sets owner to null
            // install needs owner
            this.owner = ownerCache;
            if(this.owner != null)
            {
                Chat.AddMessage("Owner not null");
            }
            if(this.owner.localUser != null)
            {
                Chat.AddMessage("Owner.localUser not null");
            }
            //base.OnInstall();
            // If I set up the userProfile again, can I just call Revoke() here?
            // And then I never have to cache it myself?
            //Revoke();
            // this is what base.Revoke() does
            // but using profile which is cached and doesn't get deleted when you complete the achievement
            
            if (profile.HasAchievement(AchievementIdentifier))
            {
                profile.RevokeAchievement(AchievementIdentifier);
            }
            profile.RevokeUnlockable(UnlockableCatalog.GetUnlockableDef(UnlockableIdentifier));
            
            
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

        void GameOver(Run run)
        {
            // reset to revoke the achievement
            Reset();
        }

    }
}
