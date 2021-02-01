using System;
using BepInEx;
using RoR2;
using RoR2.UI;
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
using MiniRpcLib;
using MiniRpcLib.Action;

// Networking stuff. Tutorials and examples
// https://github.com/risk-of-thunder/R2Wiki/wiki/Networking-&-Multiplayer-mods-(MiniRPCLib)



namespace Tasks
{
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency(MiniRpcPlugin.Dependency)]
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

        //Task[] allTasks;
        //Task[] currentTasks;

        public static event Action<int, int> OnActivate;
        public static event Action<int> OnDeactivate;
        public static event Action OnResetAll;
        public static event Action<int> OnPopup;

        public IRpcAction<int> taskCompletionClient { get; set; }
        //public IRpcAction<int[]> updateTasksClient { get; set; }
        public IRpcAction<TaskInfo> updateTaskClient { get; set; }

        Dictionary<uint, CharacterMaster> playerDict;
        // kinda bad form. There's already an array that holds the CharacterMasters. Why do I need to copy them?
        // I guess I can't guarentee it stays in the same order
        static List<CharacterMaster> playerCharacterMasters;
        int totalNumPlayers = 0;

        int numTasks;
        Reward[] rewards;
        List<TempItem>[] TempItemLists;

        // Client
        TaskInfo[] currentTasks;
        GameObject[] tasksUIObjects;

        bool activated = false;

        HUD hud;
        ObjectivePanelController panel;
        GameObject oTrackerPrefabCopy;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Awake is automatically called by Unity")]
        private void Awake() //Called when loaded by BepInEx.
        {
            Chat.AddMessage("Loaded Task plugin");

            /// ========= Client Stuff ==============================
            UnlockablesAPI.AddUnlockable<AirKills>(true);
            // AirKills.OnCompletion doesn't get called on the clients I don't believe.
            AirKills.OnCompletion += TaskCompletion;

            //NetworkServer.clien
            
            /// ========= Server Stuff ==============================
            Run.onRunStartGlobal += GameSetup;



            /// ========= Client Stuff ==============================
            // Can this live in awake?
            // or should I put it in the cosntructor
            var miniRpc = MiniRpc.CreateInstance(GUID);
            taskCompletionClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, int task) =>
            {
                // code that runs on the client
                // user specifies which user I believe so I don't have to check
                Chat.AddMessage($"Trying to make the popup on the client. User: {user} Task: {task}");
                OnPopup?.Invoke(task);
                RemoveObjectivePanel(task);
            });

            // Server sends the list of tasks to all clients
            // How do I send the data? an array of (int)TaskTypes?
            // array is not serializable
            // [Error  : Unity Log] NotSupportedException: Type System.Int32[] is not a valid argument type. If this is a type of yours, please implement INetworkSerializable.
            // try a list instead?
            // Or just have it work for a single task at a time and have the server call it multiple times
            // Justs swapping it now so I don't get errors
            // updateTasksClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, int[] tasks) =>
            updateTaskClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, TaskInfo taskInfo) =>
            {
                Chat.AddMessage($"UpdateTaskClient: {taskInfo}");
                if(currentTasks is null || currentTasks.Length != taskInfo.total)
                {
                    currentTasks = new TaskInfo[taskInfo.total];
                }

                currentTasks[taskInfo.index] = taskInfo;


                UpdateTasksUI();
            });

            // do this instead?
            // this would go in awake
            //On.RoR2.UI.ObjectivePanelController.Update += somehting?

            // option 3
            /*
            // Doesn't work at awake
            GameObject myPrefab = panel.objectiveTrackerPrefab;
            myPrefab.SetFieldValue("cachedString", "Some text");
            panel.InvokeMethod("AddObjectiveTracker", panel.objectiveTrackerPrefab);
            */
            

            On.RoR2.UI.HUD.Awake += (self, orig) =>
            {
                self(orig);
                hud = orig;

                // this is null at awake
                // so none of this runs
                panel = FindObjectOfType<ObjectivePanelController>();
                if (panel != null)
                {
                    GameObject go = Instantiate(panel.objectiveTrackerPrefab, orig.objectivePanelController.transform);
                    oTrackerPrefabCopy = go;
                    if (go is null)
                    {
                        Chat.AddMessage("Game object was not instantiated");
                        return;
                    }

                    TMPro.TextMeshProUGUI textMeshLabel = go.transform.Find("Label").GetComponent<TMPro.TextMeshProUGUI>();
                    if(textMeshLabel != null)
                    {
                        textMeshLabel.text = "Test Text";
                        textMeshLabel.color = Color.blue;
                        Chat.AddMessage($"Everything worked. GO trans: {go.transform.position}");
                    }
                    else
                    {
                        Chat.AddMessage("text mesh was null at awake");
                    }
                    /// stuff to hook my transform below?
                    //On.RoR2.UI.ChargeIndicatorController
                    //On.RoR2.UI.DifficultyBarController

                }
                else
                {
                    Chat.AddMessage("panel was null at awake");
                }

                // Create my UI
                // parent it to the right position. I wonder if that will make it move as the game objectives container scales? Or will I have to do that?
                // do myRect.transform instead of just transform
                //transform.SetParent(orig.objectivePanelController.transform, false);
                
            };
            // From corpseBloom mod example. But there is no Start.....
            //Add reserveUI to HealthBar
            /*
            On.RoR2.UI.HUD.Start += (self, orig) =>
            {
                self(orig);
                initializeReserveUI();
                reserveRect.transform.SetParent(orig.healthBar.transform, false);
                hpBar = orig.healthBar;
            };
            */
        }

        private void UpdateTasksUI()
        {
            if(panel is null)
                panel = FindObjectOfType<ObjectivePanelController>();

            if(tasksUIObjects is null || tasksUIObjects.Length != currentTasks.Length)
            {
                tasksUIObjects = new GameObject[currentTasks.Length];
            }

            if (panel != null)
            {
                for (int i = 0; i < currentTasks.Length; i++)
                {
                    if (currentTasks[i] is null)
                        break;

                    tasksUIObjects[i] = Instantiate(panel.objectiveTrackerPrefab, hud.objectivePanelController.transform);
                    if (tasksUIObjects[i] is null)
                    {
                        Chat.AddMessage("Task UI object was not instantiated");
                        return;
                    }
                    tasksUIObjects[i].SetActive(true);
                    TMPro.TextMeshProUGUI textMeshLabel = tasksUIObjects[i].transform.Find("Label").GetComponent<TMPro.TextMeshProUGUI>();
                    if (textMeshLabel != null)
                    {
                        textMeshLabel.text = currentTasks[i].description;
                    }
                }
            }
            else
            {
                Chat.AddMessage("Panel is null");
            }
        }

        void TrySpawnObjectivePanel()
        {

            // HOLY SHIT THIS ACTUALLY WORKS
            // Stuff gets added to the list
            // and when I activate the tp, the tp stuff gets added to the top of the list (above my stuff)
            // and it auto scales
            // It gives my stuff a check box which I may or may not want


            if(panel is null)
                panel = FindObjectOfType<ObjectivePanelController>();

            if (panel != null)
            {
                oTrackerPrefabCopy = Instantiate(panel.objectiveTrackerPrefab, hud.objectivePanelController.transform);
                if (oTrackerPrefabCopy is null)
                {
                    Chat.AddMessage("Game object was not instantiated");
                    return;
                }
                oTrackerPrefabCopy.SetActive(true);
                TMPro.TextMeshProUGUI textMeshLabel = oTrackerPrefabCopy.transform.Find("Label").GetComponent<TMPro.TextMeshProUGUI>();
                if (textMeshLabel != null)
                {
                    textMeshLabel.text = "Test Text";
                    //textMeshLabel.color = Color.blue;
                    Chat.AddMessage($"Everything worked. oTrackerCopy trans: {oTrackerPrefabCopy.transform.position}");
                }
                else
                {
                    Chat.AddMessage("text mesh was null");
                }
                /// stuff to hook my transform below?
                //On.RoR2.UI.ChargeIndicatorController
                //On.RoR2.UI.DifficultyBarController

            }
            else
            {
                Chat.AddMessage("panel was null");
            }
        }

        void TryRemoveObjectivePanel()
        {
            Chat.AddMessage("Relevant to false");
            /*
            [Info   : Unity Log] Pressed F9
            [Info   : Unity Log] Relevant to false
            [Error  : Unity Log] NullReferenceException: Object reference not set to an instance of an object
            Stack trace:
            Tasks.TasksPlugin.TryRemoveObjectivePanel () (at <46d8c65121e141b08ec2478de3748596>:0)
            Tasks.TasksPlugin.Update () (at <46d8c65121e141b08ec2478de3748596>:0)
             */
            oTrackerPrefabCopy.GetComponent<ObjectivePanelController.ObjectiveTracker>().isRelevant = false;
        }

        void TryRemoveObjectivePanel2()
        {
            Chat.AddMessage("Active to false");
            // ONLY ONE THAT WORKED
            oTrackerPrefabCopy.SetActive(false);
        }
        void TryRemoveObjectivePanel3()
        {
            Chat.AddMessage("Retired");
            /*
            [Info   : Unity Log] Pressed F11
            [Info   : Unity Log] Retired
            [Error  : Unity Log] NullReferenceException: Object reference not set to an instance of an object
            Stack trace:
            Tasks.TasksPlugin.TryRemoveObjectivePanel3 () (at <46d8c65121e141b08ec2478de3748596>:0)
            Tasks.TasksPlugin.Update () (at <46d8c65121e141b08ec2478de3748596>:0)
             */
            // This is a class in a class
            // It can't be a component.....
            // it doesn't even extend monobehaviour
            oTrackerPrefabCopy.GetComponent<ObjectivePanelController.ObjectiveTracker>().Retire();
        }

        void RemoveObjectivePanel(int taskType)
        {
            for (int i = 0; i < tasksUIObjects.Length; i++)
            {
                if(currentTasks[i].taskType == taskType)
                {
                    // Do something to show it was completed other than just hiding it
                    tasksUIObjects[i].SetActive(false);
                }
            }
        }

        void GameSetup(Run run)
        {
            // run.livingPlayerCount
            // run.participatingPlayerCount is this the total players?
            if(!NetworkServer.active)
            {
                // this is the client
                return;
            }

            Chat.AddMessage($"Number of players: {run.participatingPlayerCount} Living Players: {run.livingPlayerCount}");
            totalNumPlayers = run.participatingPlayerCount;
            playerDict = new Dictionary<uint, CharacterMaster>();
            playerCharacterMasters = new List<CharacterMaster>();
            
            PopulatePlayerDictionary();

            PopulateTempItemLists();

            numTasks = Enum.GetNames(typeof(TaskType)).Length;
            rewards = new Reward[numTasks];
            GenerateTasks(1);
            // How to generate tasks later maybe
            // RoR2.UI.ObjectivePanelController
            // Line 102
            // TeleporterInteraction instance = TeleporterInteraction.instance;
            // if (instance.isCharged && !instance.isInFinalSequence)
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
                    // this should probably only work on the server
                    OnActivate(1, totalNumPlayers);
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
                //CmdGiveMyselfItem();
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
                //TrySpawnObjectivePanel();
                
            }
            if(Input.GetKeyDown(KeyCode.F8))
            {
                Chat.AddMessage("Pressed F8");
                RemoveObjectivePanel((int)TaskType.AirKills);

                // DOES NOT WORK
                /*
                [Info   : Unity Log] Pressed F8
                [Error  : Unity Log] Exception: Could not find FieldInfo on UnityEngine.GameObject with the name cachedString
                Stack trace:
                R2API.Utils.Reflection+<>c__DisplayClass18_0.<GetFieldCached>b__0 (System.ValueTuple`2[T1,T2] x) (at <9e56c0ad50a94360869661ef606f8608>:0)
                R2API.Utils.Reflection.GetOrAddOnNull[TKey,TValue] (System.Collections.Concurrent.ConcurrentDictionary`2[TKey,TValue] dict, TKey key, System.Func`2[T,TResult] factory) (at <9e56c0ad50a94360869661ef606f8608>:0)
                R2API.Utils.Reflection.GetFieldCached (System.Type T, System.String name) (at <9e56c0ad50a94360869661ef606f8608>:0)
                R2API.Utils.Reflection.SetFieldValue[TValue] (System.Object instance, System.String fieldName, TValue value) (at <9e56c0ad50a94360869661ef606f8608>:0)
                Tasks.TasksPlugin.Update () (at <46d8c65121e141b08ec2478de3748596>:0)
                 
                GameObject myPrefab = panel.objectiveTrackerPrefab;
                // will this work before I instantiate it?
                // also only works if isDirty is true. I don't know if it will be
                myPrefab.SetFieldValue("cachedString", "Some text");
                panel.InvokeMethod("AddObjectiveTracker", panel.objectiveTrackerPrefab);
                */
            }
            if(Input.GetKeyDown(KeyCode.F9))
            {
                Chat.AddMessage("Pressed F9");
                // Nope
                //TryRemoveObjectivePanel();
            }
            if (Input.GetKeyDown(KeyCode.F10))
            {
                Chat.AddMessage("Pressed F10");
                //TryRemoveObjectivePanel2();
            }
            if (Input.GetKeyDown(KeyCode.F11))
            {
                Chat.AddMessage("Pressed F11");
                // Nope
                //TryRemoveObjectivePanel3();
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
                    //DealDamageInTime d = (DealDamageInTime)allTasks[1];
                    //d.OnDamage(self.damageDealt);
                }
            };
        }

        void PopulatePlayerDictionary()
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

        void PopulateTempItemLists()
        {

            TempItemLists = new List<TempItem>[CharacterMaster.readOnlyInstancesList.Count];
            Chat.AddMessage($"Trying to create {CharacterMaster.readOnlyInstancesList.Count} temp item lists. Created {TempItemLists.Length}");
            // [Info   : Unity Log] Trying to create 1 temp item lists. Created 1

            for (int i = 0; i < TempItemLists.Length; i++)
            {
                TempItemLists[i] = new List<TempItem>();
            }
        }

        void GenerateTasks(int numTasks)
        {

            //StartTasks(1);
            StartCoroutine(StartTasksWorkaround(numTasks));
        }

        IEnumerator StartTasksWorkaround(int numTasks)
        {
            // If I start tasks right at the beginning, the player's body is null
            yield return new WaitForSeconds(3);
            StartTasks(numTasks);
        }

        void StartTasks(int numTasks)
        {
            int[] taskIDNumbers = new int[numTasks];
            for (int i = 0; i < numTasks; i++)
            {
                // 0 in the enum is the base case
                int r = UnityEngine.Random.Range(1, Enum.GetNames(typeof(TaskType)).Length);

                // check to see if the tasks are unique
                // current system probably doesn't work with multiple tasks of the same type.
                // Is there anything I can do about that?

                taskIDNumbers[i] = r;

                rewards[r] = CreateRandomReward();
                Chat.AddMessage(String.Format("Task: {0}. Reward: {1} From r: {2}", ((TaskType)r).ToString(), rewards[r].ToString(), r));
                // [Info   : Unity Log] Task: AirKills. Reward: TempItem, ArmorReductionOnHit

                OnActivate?.Invoke(r, totalNumPlayers);
            }

            Chat.AddMessage("Trying to make a notification");
            Notification n = gameObject.AddComponent<Notification>();
            n.enabled = false;
            n.GetDescription = () => "1920/2, 1080/2";
            n.GetTitle = () => "Title: Middle";
            //n.SetPosition(new Vector3(5f, 1f, 0));
            //n.GenericNotification.GetComponent<RectTransform>().SetParent(NotificationQueue.readOnlyInstancesList[0].GetComponent<RectTransform>(), false);
            //n.Parent = NotificationQueue.readOnlyInstancesList[0].transform;
            n.enabled = true;

            // Bottom middle of the notification is the center of the screen
            // Text is left aligned to the box so the text isn't in the center
            StartCoroutine(MoveNotificationLater(n, new Vector3(1920f/2, 1080f/2, 0)));

            //CameraRigController.readOnlyInstancesList[0].hud.objectivePanelController.objectiveTrackerContainer

            //TestNotifications();

            //Chat.AddMessage("Picking up some items");
            PickupIndex p = new PickupIndex(ItemIndex.Hoof);
            //PickupIndex p2 = PickupCatalog.FindPickupIndex(ItemIndex.Hoof.ToString());
            if (p != null)
            {
                //Chat.AddMessage("P");
                // This works
                //NotificationQueue.readOnlyInstancesList[0].OnPickup(GetPlayerCharacterMaster(0), p);
                //Chat.AddMessage($"Positions: {NotificationQueue.readOnlyInstancesList[0].}")
                // this.currentNotification.GetComponent<RectTransform>().SetParent(base.GetComponent<RectTransform>(), false);
            }
            /*
            if (p2 != null)
            {
                //Chat.AddMessage("P2");
                //NotificationQueue.readOnlyInstancesList[0].OnPickup(GetPlayerCharacterMaster(0), p2);
            }
            */

            // taskCompletionClient.Invoke((int)taskType, NetworkUser.readOnlyInstancesList[playerNum]);
            for (int i = 0; i < totalNumPlayers; i++)
            {

                if(NetworkUser.readOnlyInstancesList is null)
                {
                    Chat.AddMessage("List is null");
                    return;
                }
                // Send a list of all tasks to all players
                if (NetworkUser.readOnlyInstancesList.Count > 0)
                {
                    Chat.AddMessage($"Count is {NetworkUser.readOnlyInstancesList.Count}");
                    // the ?. seems to fix the null errors.
                    // But why does this one not work when the taskCompleted one does?
                    //TaskInfo info = new TaskInfo()
                    //updateTaskClient?.Invoke(taskIDNumbers[0], NetworkUser.readOnlyInstancesList[i]);
                    for (int j = 0; j < taskIDNumbers.Length; j++)
                    {
                        TaskInfo info = new TaskInfo(taskIDNumbers[j], GetTaskDescription(taskIDNumbers[j]), false, j, taskIDNumbers.Length);
                        updateTaskClient?.Invoke(info, NetworkUser.readOnlyInstancesList[i]);
                    }
                }
                else
                {
                    Chat.AddMessage("No network users");
                }
            }
        }

        string GetTaskDescription(int taskType)
        {
            switch((TaskType)taskType)
            {
                case TaskType.AirKills:
                    return AirKills.description;
            }

            return "";
        }

        private void TestNotifications()
        {
            for (int i = 0; i < 11; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    int tempi = i;
                    int tempj = j;

                    GameObject go = new GameObject($"TestNotification {tempi}{tempj}");
                    //go.transform.position = NotificationQueue.readOnlyInstancesList[0].transform.position + new Vector3(i / 3f, j / 3f, 0);
                    Notification n = go.AddComponent<Notification>();
                    n.enabled = false;
                    n.GetDescription = () => $"Description: {tempi*200}, {tempj*200}";
                    n.GetTitle = () => $"Title: {tempi}, {tempj}";
                    //n.SetPosition(new Vector3(i/3f, j/3f, 0));
                    //n.Parent = NotificationQueue.readOnlyInstancesList[0].transform;
                    //n.Parent = go.transform;
                    n.enabled = true;

                    // Seems like these correspond to 1920x1080
                    StartCoroutine(MoveNotificationLater(n, new Vector3(tempi * 200, tempj * 200, 0)));
                    //StartCoroutine(SetupParentLater(n, go.transform));
                    //n.enabled = true;
                    // I think awake gets called last
                    // so there is no rootObject to move
                    // I think I need to setn.Parent to the right pos
                    // parent is Parent = RoR2Application.instance.mainCanvas.transform;
                    // but awake resets it
                }
            }
        }

        IEnumerator SetupParentLater(Notification n, Transform parent)
        {
            // this.currentNotification.GetComponent<RectTransform>().SetParent(base.GetComponent<RectTransform>(), false);

            yield return null;
            n.Parent = parent;
        }

        IEnumerator MoveNotificationLater(Notification n, Vector3 movement, bool overTime=false)
        {
            //Chat.AddMessage("Trying to move the notification");
            yield return new WaitForSeconds(0.2f);
            

            // move over time
            if (overTime)
            {
                while (n != null)
                {
                    n.GenericNotification.transform.position += movement;
                    yield return null; // skip a frame
                    yield return null;
                }
            }
            else
            {
                n.GenericNotification.transform.position = movement;
            }
            //n.SetPosition(movement);
        }

        void TaskCompletion(TaskType taskType, int playerNum)
        {
            
            Chat.AddMessage("SERVER: Player "+playerNum + " completed task " + taskType.ToString());
            // this works at least
            activated = false;
            //GiveRandomItem(netID);
            GiveReward(taskType, playerNum);
            //RpcTaskCompletion(taskType, playerNum);
            // This should send the message to the client
            // Why is it backwards from how I wrote it? Weird
            // Does this run on each client or jsut the specific one?
            taskCompletionClient.Invoke((int)taskType, NetworkUser.readOnlyInstancesList[playerNum]);
        }

        [ClientRpc]
        void RpcTaskCompletion(TaskType taskType, int playerNum)
        {
            Chat.AddMessage("CLIENT: Player " + playerNum + " completed task " + taskType.ToString() + ". Is local? "+ GetPlayerCharacterMaster(playerNum).isLocalPlayer);
            // if playerNum is me, I completed a task
            // How do I map a client to a player number?
            // show popup
            if(GetPlayerCharacterMaster(playerNum).isLocalPlayer)
                //OnPopup?.Invoke(taskType);

            taskCompletionClient.Invoke((int)taskType);
        }

        public static int GetPlayerNumber(CharacterMaster charMaster)
        {
            //playerCharacterMasters
            for (int i = 0; i < playerCharacterMasters.Count; i++)
            {
                if(playerCharacterMasters[i] == charMaster)
                {
                    return i;
                }
            }
            Chat.AddMessage("CharMaster didn't match any players");
            return -1;
        }

        public static CharacterMaster GetPlayerCharacterMaster(int playerNum)
        {
            return playerCharacterMasters[playerNum];
        }

        /*
        [Command]
        void CmdGiveMyselfItem()
        {
            // This does not work
            // Assuming index 1 is player 2 (a client)
            CharacterMaster.readOnlyInstancesList[1].inventory.GiveItem(ItemIndex.Feather);
        }
        */

        void GiveRandomItem(int playerNum)
        {
            // Do I have to do something like this?
            //playerDict[ID].inventory.CallRpcItemAdded
            playerCharacterMasters[playerNum].inventory.GiveRandomItems(1);

            // What is a reward?
            // could be an item, gold, xp, hp
        }

        void GiveReward(TaskType task, int playerNum)
        {
            if(rewards[(int)task].type == RewardType.Item)
            {
                Chat.AddMessage("Giving item: " + rewards[(int)task].item.ToString("g"));
                playerCharacterMasters[playerNum].inventory.GiveItem(rewards[(int)task].item, rewards[(int)task].numItems);
                //playerDict[ID].inventory.GiveItem(rewards[(int)task].item, rewards[(int)task].numItems);
            }
            else if(rewards[(int)task].type == RewardType.TempItem)
            {
                playerCharacterMasters[playerNum].inventory.GiveItem(rewards[(int)task].item, rewards[(int)task].numItems);
                // remove these items later
                //Stage.onServerStageComplete += RemoveTempItems;
                // Record what items to remove
                RecordTempItems(playerNum, rewards[(int)task].item, rewards[(int)task].numItems);
            }
            else
            {
                // give gold or xp
                // but for now, just give a random item
                Chat.AddMessage("Giving Random Item");
                GiveRandomItem(playerNum);
            }
        }

        void RecordTempItems(int playerNum, ItemIndex item, int count)
        {
            // ID is 6 for player 1
            // Don't know what the ID for player 2 is. Will it be 7?
            // So this old version is looking for list[6] which is out of range
            //TempItemLists[(int)ID].Add(new TempItem(item, count));
            // So maybe an array of lists was bad
            // Maybe a dict of lists would be better. Then I could use the ID

            //int playerNum = -1;
            // try to figure out which player ID matches which player in the list
            // This might be an even stupider way to do it
            /*
            for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
            {
                if(playerDict[ID] == CharacterMaster.readOnlyInstancesList[i])
                {
                    playerNum = i;
                }
            }
            */
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

        public class TaskInfo : MessageBase
        {
            public int taskType;
            public string description;
            public bool completed;
            public int index;
            public int total;

            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(taskType);
                writer.Write(description);
                writer.Write(completed);
                writer.Write(index);
                writer.Write(total);
            }

            public override void Deserialize(NetworkReader reader)
            {
                taskType = reader.ReadInt32();
                description = reader.ReadString();
                completed = reader.ReadBoolean();
                index = reader.ReadInt32();
                total = reader.ReadInt32();
            }
            public TaskInfo(int _type, string _description, bool _completed, int _index, int _total)
            {
                taskType = _type;
                description = _description;
                completed = _completed;
                index = _index;
                total = _total;
            }

            public override string ToString() => $"TaskInfo: {taskType}, {description}, {completed}, {index}/{total}";
        }
    }
    public enum TaskType { Base, AirKills };

}
