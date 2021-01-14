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
using System.Collections;
using System.Collections.ObjectModel;

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
    [R2APISubmoduleDependency(nameof(UnlockablesAPI), nameof(ItemDropAPI))]
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
        public static event Action<int> OnDeactivate;
        public static event Action OnResetAll;
        public static event Action OnPopup;

        Dictionary<uint, CharacterMaster> playerDict;
        // kinda bad form. There's already an array that holds the CharacterMasters. Why do I need to copy them?
        // I guess I can't guarentee it stays in the same order
        List<CharacterMaster> playerCharacterMasters;

        int numTasks;
        Reward[] rewards;
        List<TempItem>[] TempItemLists;

        bool activated = false;
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Awake is automatically called by Unity")]
        private void Awake() //Called when loaded by BepInEx.
        {
            Chat.AddMessage("Loaded Task plugin");

            //TempAchievements temp = new TempAchievements();
            //temp.Awake();
            // all temp.Awake() does is:
            //UnlockablesAPI.AddUnlockable<KillBeetle>(true);
            UnlockablesAPI.AddUnlockable<AirKills>(true);
            AirKills.OnCompletion += TaskCompletion;

            //KillBeetle.OnCompletion += TaskCompletion;

            playerDict = new Dictionary<uint, CharacterMaster>();
            playerCharacterMasters = new List<CharacterMaster>();
            Run.onRunStartGlobal += PopulatePlayerDictionary;

            Run.onRunStartGlobal += PopulateTempItemLists;

            numTasks = Enum.GetNames(typeof(TaskType)).Length;
            rewards = new Reward[numTasks];
            Run.onRunStartGlobal += GenerateTasks;
            // body is null if using onRunStartGlobal
            // this is probably run once per player
            //Run.onPlayerFirstCreatedServer += GenerateTasks;

            // Might be useful to tell when stages start and end
            //Stage.onServerStageBegin
            //Stage.onServerStageComplete


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

                    OnActivate(1);
                    activated = true;
                }
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                Chat.AddMessage("Pressed F3");

                if (OnDeactivate != null)
                {
                    Chat.AddMessage("Trying to send Deactivate. Someone else completed it.");

                    OnDeactivate(1);
                    activated = false;
                }
            }
            if (Input.GetKeyDown(KeyCode.F4))
            {
                //OnPopup?.Invoke();
                Chat.AddMessage("Trying to give both players an item");
                // This works!
                // Can't be called on the client
                // Host has a NetworServer.Active be true
                CharacterMaster.readOnlyInstancesList[0]?.inventory?.GiveItem(ItemIndex.ArmorPlate);
                CharacterMaster.readOnlyInstancesList[1]?.inventory?.GiveItem(ItemIndex.ArmorPlate);
                CmdGiveMyselfItem();
                //CharacterMaster.readOnlyInstancesList[0]?.inventory?.RemoveItem(ItemIndex.ArmorPlate);

            }
            if (Input.GetKeyDown(KeyCode.F5))
            {
                Chat.AddMessage("Pressing F5");

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
            if(Input.GetKeyDown(KeyCode.F6))
            {
                Chat.AddMessage("Pressing F6");
                RemoveTempItems();
                /*
                for (int i = 0; i < BodyCatalog.bodyCount; i++)
                {
                    Chat.AddMessage(BodyCatalog.GetBodyName(i));
                }
                */
            }
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Chat.AddMessage("Pressed F1");
                // who is in the list?
                for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
                {
                    Chat.AddMessage($"Name: {CharacterMaster.readOnlyInstancesList[i].name} NetID: {CharacterMaster.readOnlyInstancesList[i].netId} LocalPlayer: {CharacterMaster.readOnlyInstancesList[i].isLocalPlayer}");
                    Chat.AddMessage($"Player: {CharacterMaster.readOnlyInstancesList[i].playerCharacterMasterController} PlayerID: {CharacterMaster.readOnlyInstancesList[i].playerControllerId}");
                }

                /*
                // Chat.AddMessage($"Player: {CharacterMaster.readOnlyInstancesList[i].playerCharacterMasterController} PlayerID: {CharacterMaster.readOnlyInstancesList[i].playerControllerId} Stats: {CharacterMaster.readOnlyInstancesList[i].playerStatsComponent}");
                // Why is playerControllerId -1 for all of them?
                [Info   : Unity Log] Pressed F7
                [Info   : Unity Log] Player: CommandoMaster(Clone) (RoR2.PlayerCharacterMasterController) PlayerID: -1 Stats: CommandoMaster(Clone) (RoR2.Stats.PlayerStatsComponent)
                [Info   : Unity Log] Player:  PlayerID: -1 Stats: 

                // Chat.AddMessage($"Name: {CharacterMaster.readOnlyInstancesList[i].name} NetID: {CharacterMaster.readOnlyInstancesList[i].netId} LocalPlayer: {CharacterMaster.readOnlyInstancesList[i].isLocalPlayer}");
                // How am I not the local player
                // And I was playing artificier...
                // This doesn't find players, just everything in the scene
                [Info: Unity Log] Pressed F7
                [Info: Unity Log] Name: CommandoMaster(Clone) NetID: 6 LocalPlayer: False
                [Info: Unity Log] Name: BeetleMaster(Clone) NetID: 58 LocalPlayer: False
                [Info: Unity Log] Name: GolemMaster(Clone) NetID: 60 LocalPlayer: False
                [Info: Unity Log] Name: WispMaster(Clone) NetID: 62 LocalPlayer: False
                [Info: Unity Log] Pressed F7
                [Info: Unity Log] Name: CommandoMaster(Clone) NetID: 6 LocalPlayer: False
                [Info: Unity Log] Name: BeetleMaster(Clone) NetID: 58 LocalPlayer: False
                [Info: Unity Log] Name: LemurianMaster(Clone) NetID: 91 LocalPlayer: False
                [Info: Unity Log] Name: BeetleMaster(Clone) NetID: 102 LocalPlayer: False
                */
            }
            if(Input.GetKeyDown(KeyCode.F7))
            {
                Chat.AddMessage("Pressed F7");
                CmdGiveMyselfItem();
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
                Chat.AddMessage($"Name: {CharacterMaster.readOnlyInstancesList[i].name} NetID: {CharacterMaster.readOnlyInstancesList[i].netId} LocalPlayer: {CharacterMaster.readOnlyInstancesList[i].isLocalPlayer}");
                playerDict[CharacterMaster.readOnlyInstancesList[i].netId.Value] = CharacterMaster.readOnlyInstancesList[i];

                if(CharacterMaster.readOnlyInstancesList[i].playerCharacterMasterController != null)
                {
                    //characterMasters
                    playerCharacterMasters.Add(CharacterMaster.readOnlyInstancesList[i]);
                }
            }

            // Only runs the loop once
            //[Info: Unity Log] Trying to fill dictionary
            //[Info   : Unity Log] Name: CommandoMaster(Clone) NetID: 6 LocalPlayer: False
            // localPlayer being false is weird
        }

        void PopulateTempItemLists(Run run)
        {

            TempItemLists = new List<TempItem>[CharacterMaster.readOnlyInstancesList.Count];
            Chat.AddMessage($"Trying to create {CharacterMaster.readOnlyInstancesList.Count} temp item lists. Created {TempItemLists.Length}");
            // [Info   : Unity Log] Trying to create 1 temp item lists. Created 1

            for (int i = 0; i < TempItemLists.Length; i++)
            {
                TempItemLists[i] = new List<TempItem>();
            }
        }

        void GenerateTasks(Run run)
        {

            //StartTasks(1);
            StartCoroutine(StartTasksWorkaround(1));
        }

        IEnumerator StartTasksWorkaround(int numTasks)
        {
            // If I start tasks right at the beginning, the player's body is null
            yield return new WaitForSeconds(3);
            StartTasks(numTasks);
        }

        void StartTasks(int numTasks)
        {
            for (int i = 0; i < numTasks; i++)
            {
                // 0 in the enum is the base case
                int r = UnityEngine.Random.Range(1, Enum.GetNames(typeof(TaskType)).Length);

                rewards[r] = CreateRandomReward();
                Chat.AddMessage(String.Format("Task: {0}. Reward: {1} From r: {2}", ((TaskType)r).ToString(), rewards[r].ToString(), r));
                // [Info   : Unity Log] Task: AirKills. Reward: TempItem, ArmorReductionOnHit

                OnActivate?.Invoke(r);
            }
        }

        void TaskCompletion(TaskType taskType, uint netID)
        {
            
            Chat.AddMessage(playerDict[netID].name + " completed task " + taskType.ToString());
            // this works at least
            activated = false;
            //GiveRandomItem(netID);
            GiveReward(taskType, netID);
        }

        [Command]
        void CmdGiveMyselfItem()
        {
            // This does not work
            // Assuming index 1 is player 2 (a client)
            CharacterMaster.readOnlyInstancesList[1].inventory.GiveItem(ItemIndex.Feather);
        }

        void GiveRandomItem(uint ID)
        {
            // Do I have to do something like this?
            //playerDict[ID].inventory.CallRpcItemAdded
            playerDict[ID].inventory.GiveRandomItems(1);

            // What is a reward?
            // could be an item, gold, xp, hp
        }

        void GiveReward(TaskType task, uint ID)
        {
            if(rewards[(int)task].type == RewardType.Item)
            {
                Chat.AddMessage("Giving item: " + rewards[(int)task].item.ToString("g"));
                playerDict[ID].inventory.GiveItem(rewards[(int)task].item, rewards[(int)task].numItems);
            }
            else if(rewards[(int)task].type == RewardType.TempItem)
            {
                playerDict[ID].inventory.GiveItem(rewards[(int)task].item, rewards[(int)task].numItems);
                // remove these items later
                //Stage.onServerStageComplete += RemoveTempItems;
                // Record what items to remove
                RecordTempItems(ID, rewards[(int)task].item, rewards[(int)task].numItems);
            }
            else
            {
                // give gold or xp
                // but for now, just give a random item
                Chat.AddMessage("Giving Random Item");
                GiveRandomItem(ID);
            }
        }

        void RecordTempItems(uint ID, ItemIndex item, int count)
        {
            // ID is 6 for player 1
            // Don't know what the ID for player 2 is. Will it be 7?
            // So this old version is looking for list[6] which is out of range
            //TempItemLists[(int)ID].Add(new TempItem(item, count));
            // So maybe an array of lists was bad
            // Maybe a dict of lists would be better. Then I could use the ID

            int playerNum = -1;
            // try to figure out which player ID matches which player in the list
            // This might be an even stupider way to do it
            for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
            {
                if(playerDict[ID] == CharacterMaster.readOnlyInstancesList[i])
                {
                    playerNum = i;
                }
            }
            if(playerNum < 0)
            {
                Chat.AddMessage("Didn't find a match. Couldn't record items");
                return;
            }
            // Adding glasses x5 and adding glasses x3 will just make 2 entries in the list
            // instead of having one entry for glasses x8
            // probably not a big deal to make it work that way
            TempItemLists[playerNum].Add(new TempItem(item, count));
        }

        void RemoveTempItems()
        {
            // Something here goes out of range
            Chat.AddMessage($"Character list: {CharacterMaster.readOnlyInstancesList.Count} playerCharList: {playerCharacterMasters.Count} TempItemLists array: {TempItemLists.Length} Expect 1 for all");
            // this list counts mobs as well as players
            // players have
            //CharacterMaster.readOnlyInstancesList[0].playerCharacterMasterController
            // but mobs don't
            for (int i = 0; i < playerCharacterMasters.Count; i++)
            {

                List<TempItem> list = TempItemLists[i];
                int count = 0;
                Chat.AddMessage($"List count: {list.Count}");
                while(list.Count > 0)
                {
                    count++;
                    if (count > 50)
                    {
                        Chat.AddMessage("Oops. Infinite loop. Quitting remove temp items");
                        return;
                    }
                        
                    TempItem temp = list[0];
                    Chat.AddMessage($"Removing {temp.count} {temp.item:g}");
                    // will this take items from the right players? I dunno
                    // Will it break what's in my dict? dunno
                    // are the CharacterMasters in the dict copies or references?
                    // appears to work for 1 player

                    // using my own playerCharacter cache
                    // CharacterMaster.readOnlyInstanceList counts mobs too
                    playerCharacterMasters[i].inventory.RemoveItem(temp.item, temp.count);
                    // player dict needs 6 instead of 0
                    //playerDict[(uint)i].inventory.RemoveItem(temp.item, temp.count);
                    list.RemoveAt(0);
                }
                Chat.AddMessage($"Num times loop ran: {count}");
            }
            /*
            Running with no temp items
            [Info: Unity Log] Pressing F6
            [Info   : Unity Log] Character list: 5 TempItemLists array: 1 Expect 1 for both
            [Info: Unity Log] List count: 0
            [Info: Unity Log] Num times loop ran: 0
            [Error: Unity Log] IndexOutOfRangeException: Index was outside the bounds of the array.
            Stack trace:
            Tasks.TasksPlugin.RemoveTempItems()(at<a4c4adfe592245d58f7cf0d692b5c5bd>:0)
            Tasks.TasksPlugin.Update()(at<a4c4adfe592245d58f7cf0d692b5c5bd>:0)
            
            //running with temp items
            [Info   : Unity Log] Pressing F6
            [Info   : Unity Log] Character list: 4 TempItemLists array: 1 Expect 1 for both
            [Info   : Unity Log] List count: 1
            [Info   : Unity Log] Removing 5 WardOnLevel
            [Info   : Unity Log] Num times loop ran: 1
            [Error  : Unity Log] IndexOutOfRangeException: Index was outside the bounds of the array.
            Stack trace:
            Tasks.TasksPlugin.RemoveTempItems () (at <a4c4adfe592245d58f7cf0d692b5c5bd>:0)
            Tasks.TasksPlugin.Update () (at <a4c4adfe592245d58f7cf0d692b5c5bd>:0)
            */
        }

        Reward CreateRandomReward()
        {
            int r = UnityEngine.Random.Range(0, 1);// Enum.GetNames(typeof(RewardType)).Length);
            RewardType type = (RewardType)1;

            int item = UnityEngine.Random.Range(0, Enum.GetNames(typeof(ItemIndex)).Length);


            // get a list of all tier 1 healing items
            //List<ItemIndex> healingItemsTier1 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier1, ItemTag.Healing);
            /*
                var dropList = Run.instance.availableTier1DropList;
                //Debug.Log($"Drop list count is {dropList.Count}");
                var nextItem = Run.instance.treasureRng.RangeInt(0, dropList.Count);

                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // mushroom is 5
                // tri tip is 7
                nextItem = 7;
                PickupDropletController.CreatePickupDroplet(dropList[nextItem], transform.position, transform.forward * 20f);
            */
            Reward reward = new Reward(type, (ItemIndex)item, (type == RewardType.TempItem) ? 5 : 1, false, 100, 100);
            return reward;
        }

        public struct Reward
        {
            public Reward(RewardType _type, ItemIndex _item, int _numItems, bool _temporary, int _gold, int _xp)
            {
                type = _type;
                item = _item;
                numItems = _numItems;
                temporary = _temporary;
                gold = _gold;
                xp = _xp;
            }

            public RewardType type;
            public ItemIndex item;
            public int numItems;
            public bool temporary;
            public int gold;
            public int xp;

            public override string ToString() => $"{type.ToString("g")}, {item.ToString("g")}";
        }

        public struct TempItem
        {
            public TempItem(ItemIndex _item, int _count)
            {
                item = _item;
                count = _count;
            }
            public ItemIndex item;
            public int count;
        }

        public enum RewardType { Item, TempItem, Gold, Xp };
    }
    public enum TaskType { Base, AirKills };

}
