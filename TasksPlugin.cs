using System;
using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;
using R2API;
using R2API.Utils;
using System.Collections.Generic;

/// <summary>
/// Things to do: 
///     Make sure your references are located in a "libs" folder that's sitting next to the project folder.
///         This folder structure was chosen as it was noticed to be one of the more common structures.
///     Add a NuGet Reference to Mono.Cecil. The one included in bepinexpack3.0.0 on thunderstore is the wrong version 0.10.4. You want 0.11.1.
///         You can do this by right clicking your project (not your solution) and going to "Manage NuGet Packages".
///    Make sure the AUTHOR field is correct.
///    Make sure the MODNAME field is correct.
///    Delete this comment!
///    Oh and actually write some stuff.
/// </summary>



namespace Tasks
{
    [BepInDependency("com.bepis.r2api")]
    //[R2APISubmoduleDependency(nameof(yourDesiredAPI))]
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public sealed class TasksPlugin : BaseUnityPlugin
    {
        public const string
            MODNAME = "Tasks",
            AUTHOR = "Solrun",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "0.0.0";

        Task[] allTasks;
        Task[] currentTasks;

        public static event Action<int> OnActivate;
        public static event Action OnResetAll;
        public static event Action OnPopup;

        Dictionary<uint, CharacterMaster> playerDict; 
        // kinda bad form. There's already an array that holds the CharacterMasters. Why do I need to copy them?
        // I guess I can't guarentee it stays in the same order

        bool activated = false;
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Awake is automatically called by Unity")]
        private void Awake() //Called when loaded by BepInEx.
        {
            Chat.AddMessage("Loaded Task plugin");

            TempAchievements temp = new TempAchievements();
            temp.Awake();

            KillBeetle.OnCompletion += TaskCompletion;

            playerDict = new Dictionary<uint, CharacterMaster>();
            Run.onRunStartGlobal += PopulatePlayerDictionary;

            // Sounds like I'd only get local and not necessarily current
            //UserProfile.GetAvailableProfileNames()
            //UserProfile.GetProfile(name)

            

            // maybe this is how I can give people items
            //CharacterMaster.readOnlyInstancesList[0].inventory.GiveItem(ItemIndex.ArmorPlate);
            

            //CharacterMaster.readOnlyInstancesList

            //KillBeetle beetle = new KillBeetle();
            //beetle.Revoke();
            // Doesn't make it here
            // but doesn't throw any errors either....
            //Chat.AddMessage("Tried to Revoke the beetle achievement");

            //TempAchievements tempA = new TempAchievements();
            //tempA.Awake();

            /*
            allTasks = new Task[10];
            currentTasks = new Task[10];

            allTasks[0] = new StayInAir();
            allTasks[1] = new DealDamageInTime();
            
            foreach (Task t in allTasks)
            {
                //t.Init();
            }

            /*
            void Start() {
	
		        // Do I have to populate it like this?
		        // maybe I could use a file like json or xml
		        allTasks[0] = new StayInAir();
		        allTasks[1] = new DamageMultipleEnemies();
	
		        game.OnLevelLoad += RandomizeTasks();
		        game.OnTeleStarted += RandomizeTeleTasks();
		
		        // Do I subscribe to all events here
		        // and then just call the relevent tasks
		        // like
		        game.OnDamage += OnDamage
		        OnDamage()
		        {
			        foreach(Task t in currentTasks)
				        t.OnDamage()
		        }
	        }
            */

            //RandomizeTasks();
            //SetupHooks();
        }
        

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Start is automatically called by Unity")]
        private void Start() //Called at the first frame of the game.
        {

        }

        public void Update()
        {
            if(Input.GetKeyDown(KeyCode.F2))
            {
                // activate
                Chat.AddMessage("Pressed F2");
                if(OnActivate != null && !activated)
                {
                    Chat.AddMessage("Trying to send Activate");

                    OnActivate(0);
                    activated = true;
                }
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                Chat.AddMessage("Pressed F3");

                if (OnResetAll != null)
                {
                    Chat.AddMessage("Trying to send Reset");

                    OnResetAll();
                    activated = false;
                }
            }
            if (Input.GetKeyDown(KeyCode.F4))
            {
                //OnPopup?.Invoke();
                Chat.AddMessage("Trying to give myself an item");
                // This works!
                CharacterMaster.readOnlyInstancesList[0]?.inventory?.GiveItem(ItemIndex.ArmorPlate);

            }
            if(Input.GetKeyDown(KeyCode.F5))
            {
                // I think I can just omit inventory....
                string netID = CharacterMaster.readOnlyInstancesList[0].netId.ToString();
                if (!netID.IsNullOrWhiteSpace())
                    Chat.AddMessage("Player 0 net ID: " + netID);
                // string netID = CharacterMaster.readOnlyInstancesList[0].inventory.netId.ToString();
                // [Info   : Unity Log] Player 0 net ID: 6
                // Net id was the same as what was recorded in the achievements in the same game
                // 6 both times I launched the game. Is it always 6 for player 1? Is it 6 for everyone or is it 7, 8, 9?
                /*
                string myName = CharacterMaster.readOnlyInstancesList[0].GetComponent<UserProfile>().name; // this is null. So no UserProfile attached
                if (!myName.IsNullOrWhiteSpace())
                    Chat.AddMessage("My name is " + myName);
                */
            }
        }

        /*
        private void FixedUpdate()
        {
            for (int i = 0; i < currentTasks.Length; i++)
            {
                currentTasks[i].FixedUpdate();
            }
        }
        */
        void RandomizeTasks()
        {
            for (int i = 0; i < currentTasks.Length; i++)
            {
                int r = UnityEngine.Random.Range(0, allTasks.Length);
                currentTasks[i] = allTasks[r];
            }
        }

        void SetupHooks()
        {
            On.RoR2.HealthComponent.SendDamageDealt += (orig, self) =>
            {
                // check if it's a player
                // and check which player
                orig(self);
                if (self.attackerTeamIndex == TeamIndex.Player)
                {
                    DealDamageInTime d = (DealDamageInTime)allTasks[1];
                    d.OnDamage(self.damageDealt);
                }
            };
        }

        void PopulatePlayerDictionary(Run run)
        {
            Chat.AddMessage("Trying to fill dictionary");
            //CharacterMaster.readOnlyInstancesList
            for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
            {
                playerDict[CharacterMaster.readOnlyInstancesList[i].netId.Value] = CharacterMaster.readOnlyInstancesList[i];
            }
        }

        void TaskCompletion(int taskID, uint netID)
        {
            
            Chat.AddMessage(playerDict[netID].name + " completed task " + taskID);
            // this works at least
            activated = false;
            GiveReward(netID);
        }

        void GiveReward(uint ID)
        {
            // Do I have to do something like this?
            //playerDict[ID].inventory.CallRpcItemAdded
            playerDict[ID].inventory.GiveRandomItems(1);


        }
    }
}
