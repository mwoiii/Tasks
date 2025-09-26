﻿using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using RoR2.Artifacts;
using RoR2.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.UI;

// Networking stuff. Tutorials and examples
// https://github.com/risk-of-thunder/R2Wiki/wiki/Networking-&-Multiplayer-mods-(MiniRPCLib)



namespace Tasks {
    // nameof(ItemDropAPI),
    [BepInDependency("com.bepis.r2api")]
    //[R2APISubmoduleDependency(nameof(yourDesiredAPI))]
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public sealed class TasksPlugin : BaseUnityPlugin {
        public const string
            MODNAME = "Tasks",
            AUTHOR = "Solrun",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "1.2.3";

        public static TasksPlugin instance;

        public static event Action<int, int> OnActivate;
        public static event Action<int> OnDeactivate;
        public static event Action OnCancelAll;
        public static event Action<int> OnPopup;
        public static event Action<int, SkillSlot> OnAbilityUsed;

        // public IRpcAction<TaskCompletionInfo> taskCompletionClientOld { get; set; }
        // public IRpcAction<TaskCompletionInfo> taskEndedClientOld { get; set; }
        // public IRpcAction<TaskInfo> updateTaskClientOld { get; set; }
        // public IRpcAction<ProgressInfo> updateProgressClientOld { get; set; }


        static List<CharacterMaster> playerCharacterMasters;
        int totalNumPlayers = 0;

        int totalNumTasks;
        public Reward[] rewards;
        List<TempItem>[] TempItemLists;
        EquipmentIndex[] preonEventEqCache; // where your equipment is stored when the preon event starts and swaps it for a preon

        // Server
        int[] stageStartTasks;
        int[] teleStartTasks;
        Task[] taskCopies;
        bool telePlaced = false;

        // Client
        public TaskInfo[] currentTasks;
        public GameObject[] tasksUIObjects;
        public RectTransform[] tasksUIRects;
        public RectTransform[] rivalTasksUIRects; // shows the leader or next nearest if you are the leader

        HUD hud;
        ObjectivePanelController panel;
        GameObject itemIconPrefabCopy;

        bool rectWasAlreadyNull = false;
        bool rivalRectWasAlreadyNull = false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Awake is automatically called by Unity")]

        public void InvokeOnPopup(int taskType) {
            OnPopup?.Invoke(taskType);
        }

        private void Awake() //Called when loaded by BepInEx.
        {
            Chat.AddMessage("Loaded Task plugin");

            if (instance is null) {
                instance = this;
            }
            ConfigManager c = new ConfigManager();
            ConfigManager.instance.SetConfigFile(Config);
            ConfigManager.instance.Awake();

            Task.OnCompletion += TaskCompletion;
            Task.OnUpdateProgress += UpdateTaskProgress;

            Run.onRunStartGlobal += GameSetup;


            SetupNetworking();
            SetGameHooks();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Start is automatically called by Unity")]
        private void Start() //Called at the first frame of the game.
        {

        }

        public void Update() {
            bool fKeysActive = false; // for testing. Set to false for release
            if (fKeysActive) {
                /*
                 * Input doesn't work anymore?
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    PickupIndex p = new PickupIndex(RoR2Content.Items.BarrierOnKill.itemIndex);
                    PickupDropletController.CreatePickupDroplet(p, GetPlayerCharacterMaster(0).GetBody().transform.position, GetPlayerCharacterMaster(0).GetBody().transform.forward);
                }
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    TeleporterInteraction.instance.shouldAttemptToSpawnGoldshoresPortal = true;
                    TeleporterInteraction.instance.shouldAttemptToSpawnMSPortal = true;
                    TeleporterInteraction.instance.shouldAttemptToSpawnShopPortal = true;
                }
                if (Input.GetKeyDown(KeyCode.F3))
                {

                }
                if (Input.GetKeyDown(KeyCode.F4))
                {
                    Chat.AddMessage("Pressed F4");
                    // spawn a lockbox nearby
                    Xoroshiro128Plus xoroshiro128Plus = new Xoroshiro128Plus(0);

                    // "iscbrokendrone1";
                    GameObject gameObject2 = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscLockbox"), new DirectorPlacementRule
                    {
                        placementMode = DirectorPlacementRule.PlacementMode.Direct,
                        position = GetPlayerCharacterMaster(0).GetBody().transform.position + new Vector3(1, 0, 0)
                    }, xoroshiro128Plus));
                    Chat.AddMessage($"Spawned {gameObject2.name} at {gameObject2.transform.position}");

                }
                if (Input.GetKeyDown(KeyCode.F5))
                {
                    // I can call this on the server to show on all players
                    ChatMessage.Send("Do I need to add this API? No. This should go to each player");
                }
                if (Input.GetKeyDown(KeyCode.F6))
                {
                    Chat.AddMessage("Pressed F6");
                    // creating a command orb
                    //typeof(GenericPickupController).InvokeMethod("SendPickupMessage", GetPlayerCharacterMaster(playerNum), rewards[(int)task].item);
                    // turn on command I believe

                    typeof(CommandArtifactManager).InvokeMethod("OnArtifactEnabled", RunArtifactManager.instance, RoR2Content.Artifacts.commandArtifactDef);
                    if (typeof(CommandArtifactManager).GetField("commandCubePrefab") is null)
                    {
                        Chat.AddMessage("commandCubePrefab is null");
                    }
                    typeof(CommandArtifactManager).SetFieldValue<GameObject>("commandCubePrefab", Resources.Load<GameObject>("Prefabs/NetworkedObjects/CommandCube"));
                    // need to make an item droplet. Specifically have it hit the ground
                    PickupDropletController.onDropletHitGroundServer += TurnOffCommand;
                    // technically, the next droplet to hit the ground is the command droplet. Whether it was created right here or if it was in the air already when this one was spawned.

                    // icon
                    //RoR2Content.Artifacts.commandArtifactDef.smallIconSelectedSprite

                    // create a droplet
                    PickupIndex p = new PickupIndex(RoR2Content.Items.BarrierOnKill.itemIndex);
                    PickupDropletController.CreatePickupDroplet(p, GetPlayerCharacterMaster(0).GetBody().transform.position, GetPlayerCharacterMaster(0).GetBody().transform.forward);
                }
                if (Input.GetKeyDown(KeyCode.F7))
                {
                    Chat.AddMessage("Pressed F7");

                }
                */
            }

        }

        void GameSetup(Run run) {
            // clients need these


            totalNumTasks = Enum.GetNames(typeof(TaskType)).Length;
            rewards = new Reward[totalNumTasks];

            Debug.Log($"Tasks mod: Number of players: {run.participatingPlayerCount} Living Players: {run.livingPlayerCount}");
            totalNumPlayers = run.participatingPlayerCount;
            playerCharacterMasters = new List<CharacterMaster>(totalNumPlayers);

            // debugging stuff. So that it doesn't spam so much
            rectWasAlreadyNull = false;
            rivalRectWasAlreadyNull = false;

            PopulatePlayerCharaterMasterList();

            if (!NetworkServer.active) {
                // this is the client
                return;
            }

            TaskSetup();

            PopulateTempItemLists();

            /*
             * This is only run on the server
             * But it doesn't work anyway
             * And clients need this too
             * so, in updateTasksUI() that is run by everyone, they use a different way to find itemIconPrefab
            ItemInventoryDisplay display = FindObjectOfType<ItemInventoryDisplay>();
            if (display is null)
            {
                // why are you null
                Debug.Log("Display is null");
            }
            else
            {
                itemIconPrefabCopy = display.itemIconPrefab;
            }
            */

            // unhook all tasks. Cleanup from last round
            OnCancelAll?.Invoke();
        }

        void TaskSetup() {
            // How to make a new task
            // make the class
            // increment the size of the array
            // Create a new object, add it to the array
            // add a new type to the TaskType enum in Task.cs
            // update the type in the class you made
            // Update ConfigManager with the new task (kinda optional. Won't break if you don't)

            Debug.Log("Creating Task Objects");
            taskCopies = new Task[27];

            AirKills airKills = new AirKills();
            DamageMultipleTargets task2 = new DamageMultipleTargets();
            DealDamageInTime task3 = new DealDamageInTime();
            StayInAir task4 = new StayInAir();
            BiggestHit task5 = new BiggestHit();
            MostDistance task6 = new MostDistance();
            PreonEvent task7 = new PreonEvent();
            FarthestAway task8 = new FarthestAway();
            FailShrine task9 = new FailShrine();
            OpenChests task10 = new OpenChests();
            StartTeleporter task11 = new StartTeleporter();
            UsePrinters task12 = new UsePrinters();
            OrderedSkills task13 = new OrderedSkills(); // these are pretty bad tasks
            DontUseSkill task14 = new DontUseSkill(); // this too
            BabyDrone task15 = new BabyDrone();
            Die task16 = new Die();
            FindLockbox task17 = new FindLockbox();
            HealingItem task18 = new HealingItem();
            NoJump task19 = new NoJump();
            VeryBest task20 = new VeryBest();
            FewestElites task21 = new FewestElites();
            GetLucky task22 = new GetLucky();
            GetLow task23 = new GetLow();
            KillStreak task24 = new KillStreak();
            QuickDraw task25 = new QuickDraw();
            FarKill task26 = new FarKill();
            FewElites task27 = new FewElites();

            // Make the array bigger. Equal to whatever the last name is

            // -1 to ignore the base type
            taskCopies[(int)airKills.type - 1] = airKills;
            taskCopies[(int)task2.type - 1] = task2;
            taskCopies[(int)task3.type - 1] = task3;
            taskCopies[(int)task4.type - 1] = task4;
            taskCopies[(int)task5.type - 1] = task5;
            taskCopies[(int)task6.type - 1] = task6;
            taskCopies[(int)task7.type - 1] = task7;
            taskCopies[(int)task8.type - 1] = task8;
            taskCopies[(int)task9.type - 1] = task9;
            taskCopies[(int)task10.type - 1] = task10;
            taskCopies[(int)task11.type - 1] = task11;
            taskCopies[(int)task12.type - 1] = task12;
            taskCopies[(int)task13.type - 1] = task13;
            taskCopies[(int)task14.type - 1] = task14;
            taskCopies[(int)task15.type - 1] = task15;
            taskCopies[(int)task16.type - 1] = task16;
            taskCopies[(int)task17.type - 1] = task17;
            taskCopies[(int)task18.type - 1] = task18;
            taskCopies[(int)task19.type - 1] = task19;
            taskCopies[(int)task20.type - 1] = task20;
            taskCopies[(int)task21.type - 1] = task21;
            taskCopies[(int)task22.type - 1] = task22;
            taskCopies[(int)task23.type - 1] = task23;
            taskCopies[(int)task24.type - 1] = task24;
            taskCopies[(int)task25.type - 1] = task25;
            taskCopies[(int)task26.type - 1] = task26;
            taskCopies[(int)task27.type - 1] = task27;






            // Can I do something like this?
            // From RoR2.Chat.ChatMessageBase.BuildMessageTypeNetMap()
            // foreach (Type type in typeof(Chat.ChatMessageBase).Assembly.GetTypes())
            //{
            //    if (type.IsSubclassOf(typeof(Chat.ChatMessageBase)))
        }

        void PopulatePlayerCharaterMasterList() {
            for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++) {
                if (CharacterMaster.readOnlyInstancesList[i].playerCharacterMasterController != null) {
                    //characterMasters
                    playerCharacterMasters.Add(CharacterMaster.readOnlyInstancesList[i]);
                }
            }
        }

        void PopulateTempItemLists() {
            TempItemLists = new List<TempItem>[CharacterMaster.readOnlyInstancesList.Count];

            for (int i = 0; i < TempItemLists.Length; i++) {
                TempItemLists[i] = new List<TempItem>();
            }
        }

        void SetupNetworking() {
            //Debug.Log("Tasks: SetupNetworking");
            // new R2API networking API

            NetworkingAPI.RegisterMessageType<SyncTaskCompletion>();
            NetworkingAPI.RegisterMessageType<SyncTaskEnded>();
            NetworkingAPI.RegisterMessageType<SyncUpdateTask>();
            NetworkingAPI.RegisterMessageType<SyncUpdateProgress>();

            /*
            // Old miniRpc
            var miniRpc = MiniRpc.CreateInstance(GUID);
            taskCompletionClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, TaskCompletionInfo taskCompletionInfo) => {
                // code that runs on the client
                // user specifies which user so I don't have to check
                CreateNotification(taskCompletionInfo.taskType);
                OnPopup?.Invoke(taskCompletionInfo.taskType);
                // winner has this called and the basic one
                // so don't need to pass the name here and create 2 text fields
                RemoveObjectivePanel(taskCompletionInfo.taskType); //, taskCompletionInfo.winnerName
            });
            

            taskEndedClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, TaskCompletionInfo taskCompletionInfo) => {
                // task ended
                RemoveObjectivePanel(taskCompletionInfo.taskType, taskCompletionInfo.winnerName);
            });

            updateTaskClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, TaskInfo taskInfo) => {
                // this is called once for each task when tasks are created
                if (currentTasks is null || currentTasks.Length != taskInfo.total) {
                    currentTasks = new TaskInfo[taskInfo.total];
                }

                // make sure your number of UI elements matches the host's taskInfo they are giving you
                // the number of tasks is determined by the config file which can differ player-to-player
                if (taskInfo.total != tasksUIObjects.Length) {
                    tasksUIObjects = new GameObject[taskInfo.total];
                    tasksUIRects = new RectTransform[taskInfo.total];
                    rivalTasksUIRects = new RectTransform[taskInfo.total];
                }

                currentTasks[taskInfo.index] = taskInfo;
                rewards[(int)taskInfo.taskType] = taskInfo.reward;

                // make each UI element one at a time for each task
                UpdateTasksUI(taskInfo.index);
            });            

            updateProgressClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, ProgressInfo progressInfo) => {
                bool playerLeading = progressInfo.GetMyProgress() > progressInfo.GetRivalProgress();
                UpdateProgress(tasksUIRects[progressInfo.taskIndex], progressInfo.GetMyProgress());
                UpdateProgress(rivalTasksUIRects[progressInfo.taskIndex], progressInfo.GetRivalProgress(), playerLeading);
            });
            */

            //Debug.Log("Tasks: End of SetupNetworking");

        }

        void SetGameHooks() {
            //Debug.Log("Tasks: SetGameHooks");
            // Used for tasks
            On.RoR2.CharacterBody.OnSkillActivated += (orig, self, param) => {
                // triggers on npcs attacking too. Everything in the game is a skill
                // only runs if the skill is off cd
                // But it also triggers on skills you can cancel. Like engi turrets or missiles. You press R, the UI shows up
                // but then you cancel the build and the skill doesn't go on CD
                // I'm not sure if there's a general solution for that
                // only run this stuff on the server
                if (NetworkServer.active) {
                    if (self.isPlayerControlled) {
                        int i = GetPlayerNumber(self.master);
                        SkillSlot skill = self.skillLocator.FindSkillSlot(param);
                        OnAbilityUsed?.Invoke(i, skill);
                    }
                }

                orig(self, param);
            };

            GlobalEventManager.OnInteractionsGlobal += (Interactor interactor, IInteractable interactable, GameObject go) => {
                // interactor is the player
                //interactor.GetComponent<CharacterBody>();
                string interactableType = interactable.GetType().ToString();

                // Chat.AddMessage($"Interacted with {go.name} InterType: {interactableType} Components: {componentsString}");

                // drone
                // [Info   : Unity Log] Interacted with Drone1Broken(Clone) InterType: RoR2.PurchaseInteraction Components: UnityEngine.Transform UnityEngine.Networking.NetworkIdentity RoR2.Highlight RoR2.SummonMasterBehavior RoR2.PurchaseInteraction RoR2.EventFunctions RoR2.Hologram.HologramProjector RoR2.GenericDisplayNameProvider RoR2.ModelLocator 
                // Newt Altar
                // [Info   : Unity Log] Interacted with NewtStatue InterType: RoR2.PurchaseInteraction Components: UnityEngine.Transform RoR2.Highlight UnityEngine.Networking.NetworkIdentity RoR2.PurchaseInteraction RoR2.Hologram.HologramProjector RoR2.PortalStatueBehavior RoR2.UnlockableGranter 
                // Tele
                // [Info   : Unity Log] Interacted with Teleporter1(Clone) InterType: RoR2.TeleporterInteraction Components: UnityEngine.Transform UnityEngine.Networking.NetworkIdentity UnityEngine.Networking.NetworkTransform RoR2.Highlight RoR2.HoldoutZoneController RoR2.TeleporterInteraction RoR2.OutsideInteractableLocker RoR2.GenericDisplayNameProvider RoR2.CombatDirector RoR2.CombatDirector RoR2.SceneExitController RoR2.ModelLocator RoR2.CombatSquad RoR2.BossGroup RoR2.EntityStateMachine RoR2.NetworkStateMachine 
                // blue portal
                // [Info   : Unity Log] Interacted with PortalShop(Clone) InterType: RoR2.GenericInteraction Components: UnityEngine.Transform RoR2.ObjectScaleCurve UnityEngine.Networking.NetworkIdentity RoR2.GenericInteraction RoR2.SceneExitController RoR2.GenericDisplayNameProvider RoR2.ConvertPlayerMoneyToExperience 
                // leave bazaar portal
                // [Info   : Unity Log] Interacted with PortalShop InterType: RoR2.GenericInteraction Components: UnityEngine.Transform RoR2.ObjectScaleCurve UnityEngine.Networking.NetworkIdentity RoR2.GenericInteraction RoR2.SceneExitController RoR2.GenericDisplayNameProvider RoR2.ConvertPlayerMoneyToExperience 

                // Tp start
                // [Info   : Unity Log] Interacted with Teleporter1(Clone) InterType: RoR2.TeleporterInteraction Components: UnityEngine.Transform UnityEngine.Networking.NetworkIdentity UnityEngine.Networking.NetworkTransform RoR2.Highlight RoR2.HoldoutZoneController RoR2.TeleporterInteraction RoR2.OutsideInteractableLocker RoR2.GenericDisplayNameProvider RoR2.CombatDirector RoR2.CombatDirector RoR2.SceneExitController RoR2.ModelLocator RoR2.CombatSquad RoR2.BossGroup RoR2.EntityStateMachine RoR2.NetworkStateMachine 
                // Tp leave
                // [Info   : Unity Log] Interacted with Teleporter1(Clone) InterType: RoR2.TeleporterInteraction Components: UnityEngine.Transform UnityEngine.Networking.NetworkIdentity UnityEngine.Networking.NetworkTransform RoR2.Highlight RoR2.HoldoutZoneController RoR2.TeleporterInteraction RoR2.OutsideInteractableLocker RoR2.GenericDisplayNameProvider RoR2.CombatDirector RoR2.CombatDirector RoR2.SceneExitController RoR2.ModelLocator RoR2.CombatSquad RoR2.BossGroup RoR2.EntityStateMachine RoR2.NetworkStateMachine AkGameObj RoR2.HoldoutZoneController+FocusConvergenceController 

                /*
                if (go?.GetComponent<ShopTerminalBehavior>())
                {
                    // Multishops AND 3D printers
                    // [Info   : Unity Log] Interacted with multishop. InterType: RoR2.PurchaseInteraction
                    //Chat.AddMessage("Interacted with multishop. InterType: " + interactableType + " name: " + go.name);
                    // MultiShopTerminal(Clone)
                    // DuplicatorLarge(Clone) --- Duplicator(Clone)
                }
                if (go?.GetComponent<ChestBehavior>())
                {
                    // damage chest, eq chest, small chest all worked
                    // [Info   : Unity Log] Interacted with chest. InterType: RoR2.PurchaseInteraction
                    //Chat.AddMessage("Interacted with chest. InterType: " + interactableType + " name: " + go.name);
                    // CategoryChestUtility(Clone) --- CategoryChestDamage(Clone)
                    // Chest1 --- Chest2
                    // EquipmentBarrel(Clone)
                }
                if (go?.GetComponent<BarrelInteraction>())
                {
                    // [Info   : Unity Log] Interacted with a barrel. InterType: RoR2.BarrelInteraction
                    //Chat.AddMessage("Interacted with a barrel. InterType: " + interactableType + " name: " + go.name);
                    // Barrel1(Clone)
                }
                if (go?.GetComponent<PrintController>())
                {
                    //Chat.AddMessage("Interacted with a 3D printer. InterType: " + interactableType + " name: " + go.name);
                }
                */
                // when you use a 3D printer or eq drone i think or pool prob
                // PurchaseInteraction.onItemSpentOnPurchase += method
                // purchaseInteraction.gameObject.name.Contains("Duplicator")

                // it gets called when you start the tp event and again when you interact with the tp to leave
                if (go?.GetComponent<TeleporterInteraction>()) {
                    // this might be true for interacting with the end tp to switch to loop mode
                    // these don't actually seem to work
                    // can't ctrl+f to find this output in the log
                    // but StartTeleporter task uses the same logic and it works????
                    // so it seems to work, but isn't outputting anymore? Why?
                    Debug.Log("Interacted with TP. InterType: " + interactableType + " name: " + go.name);
                } else if (go?.GetComponent<SceneExitController>()) {
                    // not a tp so probably some kind of tp like blue or gold
                    // this also doesn't seem to work
                    Debug.Log($"Interacted with a SceneExitController {go.name}");
                    StageEnd();
                }

            };
            /*
            TeleporterInteraction.onTeleporterBeginChargingGlobal += (TeleporterInteraction interaction) =>
            {
                // Once the tele event starts
                // triggers on the client
                // So you interact, then wait a few secs, then this triggers, then the boss spawns
                Debug.Log("TP event started");
                // I can add additional tasks here (or maybe in OnInteraction(tele) as that runs first
                // however, that runs twice. Once to start the tp, again to leave the stage
            };
            TeleporterInteraction.onTeleporterChargedGlobal += (TeleporterInteraction interaction) =>
            {
                // when the charge hits 100%
                // triggers on the client
                Debug.Log("TP charged to 100%");

            };
            */
            TeleporterInteraction.onTeleporterFinishGlobal += (_) => {
                // Runs when you click the tele to move to the next stage (after you kill the boss and charge the tele)
                // triggers on the client
                Debug.Log("TP finished and player chose to leave");
                if (NetworkServer.active) {
                    StageEnd();
                }
            };

            /*
            BossGroup.onBossGroupDefeatedServer += (BossGroup group) =>
            {
                // this works. Timer too
                //Chat.AddMessage($"Boss defeated in {group.fixedTimeSinceEnabled} seconds");
            };
            */

            On.RoR2.Run.OnServerTeleporterPlaced += (orig, self, director, teleporter) => {
                orig(self, director, teleporter);
                //Chat.AddMessage("Placed the TP"); // shouldn't run on stages without a tp
                // this should run before HUD.Awake
                // Have to wait til HUD.Awake so stuff isn't null
                telePlaced = true;
                // telePlaced = false at end of stage to reset for the next stage
            };

            // most of ObjectivePanelController stuff runs in update
            // this runs in update
            /*
            On.RoR2.UI.ObjectivePanelController.RunEnterAnimations += (orig, self) =>
            {
                orig(self);
                
                Debug.Log("ObjPanel Enter Animation");
            };
            */

            On.RoR2.UI.HudObjectiveTargetSetter.Update += (orig, self) => {
                orig(self);

                // should I check if panel already exists?
                // might break between levels?
                if (panel == null) {
                    Debug.Log("Panel is null in HudObjSetter.Update");
                }
                if (self.objectivePanelController is null) {
                    Debug.Log("Task mod: objectivePanelController was null");
                }
                if (self.objectivePanelController) {
                    panel = self.objectivePanelController;
                }
            };

            // controls starting tasks
            On.RoR2.UI.HUD.Awake += (orig, self) => {
                // Have to remember to activate the spawned GO
                // This also gets called at the start of each stage. 
                // GenerateTasks waits 3 seconds and that seems to do it
                orig(self);
                hud = self;

                // ObjectivePanelController panel;
                // no longer exists
                // and I don't think objPanelController exists at hud.Awake
                // I think it's made a little later. So need to set panel later
                //panel = self.objectivePanelController;

                // check if there is a tele
                // skip stages with no tele (bazaar, gold coast, obliterate, acrid area, boss scav, end boss)

                int numberOfStageTasks = ConfigManager.instance.GetNumberOfTasks(totalNumPlayers);// totalNumPlayers + 2;// 5;
                // hard cap.
                // could be 7/8-12. The 8th or 9th task can overlap the press e to open chest UI
                // around 12, it will start to overlap your abilities
                numberOfStageTasks = Math.Min(numberOfStageTasks, 7);

                tasksUIObjects = new GameObject[numberOfStageTasks];
                tasksUIRects = new RectTransform[numberOfStageTasks];
                rivalTasksUIRects = new RectTransform[numberOfStageTasks];

                if (telePlaced) {
                    if (NetworkServer.active)
                        GenerateTasks(numberOfStageTasks);
                }
            };

            // problem:
            // mod kinda breaks when everyone dies.
            // the host just gets a ton of errors and needs to quit the game manually.
            // it seems to happen when the end screen appears telling you your stats
            // so maybe something here can help

            // they all trigger when you die at the same time
            // didn't get any errors sitting in the stats screen in a single player game
            // Run.OnRunDestroyGlobal seems to run when you click the button on the stats screen

            // Server
            /*
            [Info   : Unity Log] Run.BeginGameOver isWin: False
            [Info   : Unity Log] GameOverController.Awake
            [Info   : Unity Log] GameOverController.CallRpcClientGameOver
            [Info   : Unity Log] GameOverController.InvokeRpcRpcClientGameOver
            [Info   : Unity Log] RpcClientGameOver
            */

            // Client
            /*
            [Info   : Unity Log] GameOverController.Awake
            [Info   : Unity Log] GameOverController.InvokeRpcRpcClientGameOver
            [Info   : Unity Log] RpcClientGameOver
            */

            // fifth
            /*
            On.RoR2.GameOverController.RpcClientGameOver += (orig, self) =>
            {
                // is this false when the first 3 players die, then true when the last player dies?
                Debug.Log($"RpcClientGameOver Should? {self.shouldDisplayGameEndReportPanels}"); //false for 1 player when you die?? Why?
                // false even in multiplayer
                
                if (self.shouldDisplayGameEndReportPanels)
                {
                    CancelAllTasks();
                }
                
                orig(self);
            };
            */

            // second
            On.RoR2.GameOverController.Awake += (orig, self) => {
                Debug.Log("GameOverController.Awake");
                CancelAllTasks();
                orig(self);
            };

            // third
            /*
            On.RoR2.GameOverController.CallRpcClientGameOver += (orig, self) =>
            {
                Debug.Log("GameOverController.CallRpcClientGameOver");

                orig(self);
            };
            */
            /*
            On.RoR2.GameOverController.GenerateReportScreen += (orig, self, extra) =>
            {
                Debug.Log("GameOverController.GenerateReportScreen");

                orig(self, extra);
            };
            */
            // fourth
            /*
             * Broken now. Might need a different reference assembly
             * But I don't think I'm using it anyway
             * I think it was just here for debugging
            On.RoR2.GameOverController.InvokeRpcRpcClientGameOver += (orig, self, extra) =>
            {
                Debug.Log("GameOverController.InvokeRpcRpcClientGameOver");

                orig(self, extra);
            };
            */

            // first. False for 1 player game
            /*
            On.RoR2.Run.BeginGameOver += (orig, self, extra) =>
            {
                Debug.Log($"Run.BeginGameOver isWin: {extra.isWin}");
                orig(self, extra);
            };
            */



            //Debug.Log("Tasks: End of SetGameHooks");

        }

        public void UpdateTasksUI(int taskIndex, string text = "") {
            //TMPro.TextMeshProUGUI textMeshLabel = tasksUIObjects[taskIndex].transform.Find("Label").GetComponent<TMPro.TextMeshProUGUI>();

            if (tasksUIObjects[taskIndex] is null) {
                if (panel.objectiveTrackerPrefab is null) {
                    Debug.Log("Task mod: panel.objectiveTrackerPrefab was null");
                }
                if (panel.objectiveTrackerContainer is null) {
                    Debug.Log("Task mod: panel.objectiveTrackerContiner was null");
                }
                // do clients not have panel? They should
                tasksUIObjects[taskIndex] = Instantiate(panel.objectiveTrackerPrefab, panel.objectiveTrackerContainer.transform.parent); // old: hud.objectivePanelController.transform
                // rewards is totalNumTasks long
                // taskIndex is like 6 at most.
                if (currentTasks[taskIndex] is null) {
                    Debug.Log($"Task mod: currentTasks was null at UpdateTasksUI index: {taskIndex}");
                }
                int rewardIndex = currentTasks[taskIndex].taskType;

                if (tasksUIObjects[taskIndex] is null) {
                    Debug.Log("Task mod: Instantiate UIObject failed");
                }
                tasksUIObjects[taskIndex].SetActive(true);

                /*
                // update broke all this and I couldn't get it to work
                if (itemIconPrefabCopy is null)
                {
                    itemIconPrefabCopy = FindObjectOfType<ItemInventoryDisplay>().itemIconPrefab;
                }
                // RoR2/Base/AlienHead/AlienHead.asset
                //Addressables.LoadAssetAsync<GameObject>(key: "RoR2/Base/WarCryOnMultiKill/WarCryEffect.prefab").WaitForCompletion()

                //GameObject iconCopy = Instantiate(itemIconPrefabCopy, tasksUIObjects[taskIndex].transform);
                //ItemIcon icon = iconCopy.GetComponent<ItemIcon>();
                ItemIcon icon = Instantiate(itemIconPrefabCopy, tasksUIObjects[taskIndex].transform).GetComponent<ItemIcon>();
                //icon.image.color = Color.white;
                // old version
                //icon.SetItemIndex(rewards[rewardIndex].item.itemIndex, rewards[rewardIndex].numItems);
                icon.SetItemIndex(PickupCatalog.GetPickupDef(rewards[rewardIndex].item).itemIndex, rewards[rewardIndex].numItems);
                // SetItemIndex does this already so this shouldn't help
                //icon.image.texture = ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(rewards[rewardIndex].item).itemIndex).pickupIconTexture;
                // test
                // itemIndex is private
                //icon.SetItemIndex(itemIconPrefabCopy.GetComponent<ItemIcon>().itemIndex, rewards[rewardIndex].numItems);

                //PickupCatalog.GetPickupDef(rewards[rewardIndex].item).itemIndex
                RectTransform rect = icon.rectTransform;
                //Debug.Log($"rect is {rect} scale is {rect.localScale} pos is {rect.localPosition}");
                rect.localScale = Vector3.one * 0.5f;
                rect.localPosition = new Vector3(-113, 0, 0); // didn't work
                //Debug.Log($"after set, rect is {rect} scale is {rect.localScale} pos is {rect.localPosition}");

                //Debug.Log($"cached rect: {icon.rectTransform} scale is {icon.rectTransform.localScale} pos is {icon.rectTransform.localPosition}");
                icon.rectTransform.localScale = Vector3.one * 0.5f;
                icon.rectTransform.localPosition = new Vector3(-113, 0, 0);
                //Debug.Log($"after set, cached rect: {icon.rectTransform} scale is {icon.rectTransform.localScale} pos is {icon.rectTransform.localPosition}");

                icon.image.uvRect = new Rect(0, 0, 1, 1);
                */

                // new version
                // make my own sprite and text for x5 items
                GameObject rewardIconGO = new GameObject($"Reward Icon");
                Image i = rewardIconGO.AddComponent<Image>();
                i.sprite = ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(rewards[rewardIndex].item).itemIndex).pickupIconSprite;

                Transform labelTrans = tasksUIObjects[taskIndex].transform.Find("Label").transform;
                if (labelTrans is null) {
                    Debug.Log("Task mod: UpdateTasksUI() couldn't find label transform");
                }

                rewardIconGO.transform.SetParent(labelTrans, false);
                rewardIconGO.transform.localPosition = new Vector3(-75, 0, 0);
                rewardIconGO.transform.localScale = new Vector3(0.25f, 0.25f, 1);
                rewardIconGO.transform.localRotation = Quaternion.identity;

                if (rewards[rewardIndex].numItems > 1) {
                    GameObject stackSize = new GameObject($"Stack Size");
                    HGTextMeshProUGUI textMesh = stackSize.AddComponent<HGTextMeshProUGUI>();
                    textMesh.text = "x" + rewards[rewardIndex].numItems;

                    stackSize.transform.SetParent(labelTrans, false);
                    stackSize.transform.localPosition = new Vector3(-40, 10, 0);
                    stackSize.transform.localScale = Vector3.one * 0.3f;
                    stackSize.transform.localRotation = Quaternion.identity;
                }

                Image checkBox = tasksUIObjects[taskIndex].transform.Find("Label").Find("Checkbox").GetComponent<Image>();
                checkBox.color = new Color(0, 0, 0, 0); // invisible

                if (rewards[rewardIndex].type == RewardType.Command) {
                    checkBox.sprite = RoR2Content.Artifacts.commandArtifactDef.smallIconSelectedSprite;
                    checkBox.color = Color.white;

                    RectTransform checkRect = checkBox.rectTransform;
                    // the smaller the scale, the more to the left it is
                    checkRect.localScale = Vector3.one * 0.6f;
                    checkRect.localPosition += new Vector3(-5, -10, 0);
                    checkRect.SetAsLastSibling();
                } else if (rewards[rewardIndex].type == RewardType.Drone) {
                    string path = RewardBuilder.GetDroneTexturePath(rewards[rewardIndex].dronePath);
                    //icon.image.texture = RoR2.LegacyResourcesAPI.Load<Texture>(path);// Resources.Load<Texture>(path);
                    Texture droneTex = Resources.Load<Texture>(path);
                    Sprite droneSprite = Sprite.Create((Texture2D)droneTex, new Rect(0.0f, 0.0f, droneTex.width, droneTex.height), new Vector2(0.5f, 0.5f), 100.0f);

                    i.sprite = droneSprite;
                }
            }
            tasksUIObjects[taskIndex].SetActive(true);

            TMPro.TextMeshProUGUI textMeshLabel = tasksUIObjects[taskIndex].transform.Find("Label").GetComponent<TMPro.TextMeshProUGUI>();
            if (textMeshLabel != null) {
                textMeshLabel.text = (text == "") ? currentTasks[taskIndex].description : text;
            } else {
                Debug.Log("Task mod: UpdateTasksUI() textMeshLabel was null");

            }


            tasksUIRects[taskIndex] = CreateTaskProgressBar(textMeshLabel.transform);
            rivalTasksUIRects[taskIndex] = CreateTaskProgressBar(textMeshLabel.transform, "RivalBar");

            // both get added under the green bar. doesn't extend the task's box
            // all 3 are equidistant apart roughly. No overlapping
            // why do the player and rival overlap? they both have the same parent. Is it bc of the updating?
            //RectTransform t = CreateDummyProBar(textMeshLabel.transform, 0.5f, Color.red);
            //CreateDummyProBar(t.transform, 0.75f, Color.blue);
        }

        RectTransform CreateTaskProgressBar(Transform parent, string name = "ProBar") {
            GameObject proBar = new GameObject(name);
            RectTransform progress = proBar.AddComponent<RectTransform>();
            progress.SetParent(parent, false);
            progress.anchorMin = new Vector2(0, 0); // min and max snap the bar to the bottom of its parent
            progress.anchorMax = new Vector2(0, 0);
            progress.pivot = new Vector2(0, 0); // x needs to be 0, y doesn't matter?
            progress.sizeDelta = new Vector2(0, 3); // x is the progress. 3 is the line thickness.


            Image image = proBar.AddComponent<Image>();
            image.color = new Color(97 / 255f, 171 / 255f, 50 / 255f); // healthbar green

            return progress;
        }

        RectTransform CreateDummyProBar(Transform parent, float percent01, Color c) {
            GameObject proBar = new GameObject("TestBar");
            RectTransform progress = proBar.AddComponent<RectTransform>();
            progress.SetParent(parent, false);
            progress.anchorMin = new Vector2(0, 0);
            progress.anchorMax = new Vector2(0, 0);
            progress.pivot = new Vector2(0, 3);
            progress.sizeDelta = new Vector2(percent01 * 113, 3);

            Image image = proBar.AddComponent<Image>();
            image.color = c;

            return progress;
        }

        /// <summary>
        /// The rect to update and the percent complete (from 0 to 1)
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="percent01">Percent complete between 0 and 1</param>
        public void UpdateProgress(RectTransform rect, float percent01) {
            if (rect is null) {
                if (!rectWasAlreadyNull) {
                    // this is to suppress extra debug noise
                    // also means I'll probably never notice this error in a huge log file
                    rectWasAlreadyNull = true;
                    Debug.Log("Task mod: Rect was null");
                }
                return;
            }
            // rect can be non null, but then sizeDelta is null??
            // not sure if rect.size == null will help or if that will just push the issue elsewhere
            // still have the error with it
            // the null is triggered when quitting the game. And new games seem to work so it's low priority
            // correction: unhooks get broken
            // When you play a game, then it ends, rect isn't 'null', but it is garbage. How to check for that?
            rect.sizeDelta = new Vector2(percent01 * 113, 3); // 113 just looks about right
        }

        public void UpdateProgress(RectTransform rect, float percent01, bool playerLeading) {
            if (rect is null) {
                if (!rivalRectWasAlreadyNull) {
                    // only output this message once.
                    rivalRectWasAlreadyNull = true;
                    Debug.Log("Task mod: Rect was null in rival");
                }
                return;
            }
            // this variant is specifically for the rival rect to change the colour and layering
            Image image = rect.GetComponent<Image>();
            // what happens when players are tied? Both have 3/5 kills.
            // playerLeading will be false
            // so will make the bar blue and put it below so you probably can't see it
            // I could do percent01 * 1.00001 // +=0.001
            // 3/5 * 113 = 67.8
            // 3/5 = 0.6 + 0.01 * 113 = 68.9
            if (playerLeading) {
                image.color = new Color(234 / 255f, 229 / 255f, 135 / 255f); // barrier gold (the outline, the colour over the hp bar is see through so it's a little green)
                rect.SetAsLastSibling(); // put on top
                percent01 = Math.Max(0, percent01 - 0.02f); // might help with visuals when nearly tied
            } else {
                image.color = new Color(68 / 255f, 94 / 255f, 181 / 255f); // shield blue
                rect.SetAsFirstSibling(); // put below
                if (percent01 > 0) {
                    percent01 = Mathf.Min(1, percent01 + 0.02f); // should help visuals when exactly tied
                }
            }
            UpdateProgress(rect, percent01);
        }

        public void RemoveObjectivePanel(int taskType, string winnerName = "") {
            for (int i = 0; i < tasksUIObjects.Length; i++) {
                if (currentTasks[i].taskType == taskType) {
                    if (tasksUIObjects[i] is null) {
                        Debug.Log($"UI Object {i} was null");
                        break;
                    }

                    Transform labelTransform = tasksUIObjects[i].transform.Find("Label");
                    TMPro.TextMeshProUGUI textMeshLabel = labelTransform.GetComponent<TMPro.TextMeshProUGUI>();
                    CreateWinnerLabel(labelTransform, winnerName);

                    if (textMeshLabel is null) {
                        Debug.Log("Couldn't strikethrough");
                    } else {
                        textMeshLabel.color = Color.grey;
                        textMeshLabel.fontStyle |= TMPro.FontStyles.Strikethrough;

                        ItemIcon icon = tasksUIObjects[i].GetComponentInChildren<ItemIcon>();
                        if (icon)
                            icon.image.color = Color.grey;
                        Image checkBox = tasksUIObjects[i].transform.Find("Checkbox")?.GetComponent<Image>();
                        if (checkBox) {
                            checkBox.color = new Color(0.5f, 0.5f, 0.5f, checkBox.color.a); // grey, but keeping the alpha. The checkbox is usually invisible. This keeps that
                        }

                    }
                }
            }
        }

        void CreateWinnerLabel(Transform ogLabel, string winnerName = "") {
            if (winnerName == "")
                return;

            // old version
            //var copy = Instantiate(ogLabel, ogLabel.parent);
            var copy = Instantiate(ogLabel, ogLabel);
            copy.gameObject.name = "WinnerName";
            copy.localPosition = Vector3.zero;
            copy.localScale = Vector3.one * 1.25f;
            TMPro.TextMeshProUGUI tmpLabel = copy.GetComponent<TMPro.TextMeshProUGUI>();

            // this copies the progress bar as well
            // "ProBar" is the name of the bar. it's a child of tmpLabel
            Destroy(tmpLabel.transform.Find("ProBar").gameObject);
            // destroy the rival bar too
            Destroy(tmpLabel.transform.Find("RivalBar").gameObject);

            Destroy(tmpLabel.transform.Find("Reward Icon").gameObject);

            int rotationMultiplier = 1;
            if (winnerName == "FAILURE") {
                rotationMultiplier = -1;

                // teleporter text is
                // EE807F
                // rgb(238, 128, 127)
                tmpLabel.color = new Color(0.93f, 0.5f, 0.5f);

                // doesn't seem to work with tmpro
                //winnerName = $"<style=cIsHealth>{winnerName}</style>"; // health is light red
            }
            tmpLabel.transform.RotateAround(tmpLabel.transform.position, Vector3.forward, 15 * rotationMultiplier); // 20 looks weird at 15 char, but normal at 6 char. Maybe 15 looks good at both?
            tmpLabel.fontStyle = TMPro.FontStyles.Normal;
            tmpLabel.fontSizeMax = 18;
            //winnerName = winnerName + winnerName + winnerName + winnerName + winnerName + winnerName + winnerName + winnerName;
            winnerName = winnerName.Substring(0, Math.Min(15, winnerName.Length)); // arbitrary. Texts gets too small otherwise

            tmpLabel.text = winnerName;
            tmpLabel.enableWordWrapping = false;

            copy.SetAsLastSibling();
        }

        void DestroyTaskUI() {
            // For use when everyone dies, you go back to menu and start a new game
            // Clears the rects so the new game will make new ones
            // otherwise, they aren't null, but aren't accessible

            for (int i = 0; i < tasksUIRects.Length; i++) {
                if (tasksUIRects[i] != null)
                    Destroy(tasksUIRects[i].gameObject);
                if (rivalTasksUIRects[i] != null)
                    Destroy(rivalTasksUIRects[i].gameObject);
                if (tasksUIObjects[i] != null)
                    Destroy(tasksUIObjects[i].gameObject);
                // the rects should be children of the UIobject
                // would it work if I just deleted the objects and let the rects go with?

                tasksUIRects[i] = null;
                rivalTasksUIRects[i] = null;
                tasksUIObjects[i] = null;
            }
            Debug.Log("Destroyed UI objects and rects");
        }

        void GenerateTasks(int numTasks) {
            StartCoroutine(StartTasksWorkaround(numTasks));
        }

        IEnumerator StartTasksWorkaround(int numTasks) {
            // If I start tasks right at the beginning, the player's body is null
            yield return new WaitForSeconds(3); // 3 sec is arbitrary, but seems to work. Could fail if stages later in the run take longer to load maybe
            StartTasks(numTasks);
        }

        void StartTasks(int numTasks, bool teleTasks = false) {
            int[] taskIDNumbers = GetWeightedTasks(numTasks);// GetRandomUniqueTasks(numTasks);

            if (teleTasks) {
                teleStartTasks = taskIDNumbers;
            } else {
                stageStartTasks = taskIDNumbers;
            }

            for (int i = 0; i < taskIDNumbers.Length; i++) {
                // rewards[TaskType.DamageInTime]
                // array is as large as TaskType enum (-1)
                // but not necessarily full. Maybe only tasks 3, 5, 8 are active
                rewards[taskIDNumbers[i]] = RewardBuilder.CreateRandomReward();

                // These 2 lines for debugging
                int r = taskIDNumbers[i];
                Debug.Log(String.Format("Task: {0}. Reward: {1} From r: {2}", ((TaskType)r).ToString(), rewards[r].ToString(), r));

                // SOmething is null after this debug.log
                // in the last task
                // is the readOnlyInstancesList?

                OnActivate?.Invoke(taskIDNumbers[i], totalNumPlayers);
            }

            //for (int i = 0; i < totalNumPlayers; i++) {

            if (NetworkUser.readOnlyInstancesList is null) {
                Debug.Log("List is null");
                return;
            }

            // Send a list of all tasks to all players
            if (NetworkUser.readOnlyInstancesList.Count > 0) {
                for (int j = 0; j < taskIDNumbers.Length; j++) {
                    TaskInfo info = new TaskInfo(taskIDNumbers[j], GetTaskDescription(taskIDNumbers[j]), false, j, taskIDNumbers.Length, rewards[taskIDNumbers[j]]);
                    //updateTaskClient?.Invoke(info, NetworkUser.readOnlyInstancesList[i]);
                    //new SetupTaskMessage(info).Send(NetworkDestination.Clients);
                    new SyncUpdateTask(info.taskType, info.description, info.completed, info.index, info.total, info.reward).Send(NetworkDestination.Clients);
                }
            } else {
                Debug.Log("No network users");
            }

            //break; // testing NEtworkingAPI. I think it sends to all clients at once, not one at a time so don't need the loop
            //}
        }

        void StageEnd() {
            // Do this before ending all tasks
            // some tasks are only finished when the stage ends (deal the most damage, etc)
            // So they need to give their reward after temp items are removed in case they give temp items
            // if there was no tele, there were no tasks
            if (telePlaced) {
                RemoveTempItems();
                EndAllTasks();
            }
            telePlaced = false;
        }

        void EndAllTasks() {
            if (stageStartTasks != null) {
                for (int i = 0; i < stageStartTasks.Length; i++) {
                    OnDeactivate?.Invoke(stageStartTasks[i]);
                    RemoveObjectivePanel(stageStartTasks[i]);
                }
            }
            if (teleStartTasks != null) {
                for (int i = 0; i < teleStartTasks.Length; i++) {
                    OnDeactivate?.Invoke(teleStartTasks[i]);
                    RemoveObjectivePanel(teleStartTasks[i]);
                }
            }
        }

        void CancelAllTasks() {
            // called when the game ends before the teleporter (when all players die)
            Debug.Log("Cancel all tasks");
            OnCancelAll?.Invoke(); // is it a problem is clients call this? Shouldn't be. Clients never create the tasks so nothing is subbed to this
            telePlaced = false;
            DestroyTaskUI();
        }

        int[] GetWeightedTasks(int count) {
            int[] results = new int[count];
            WeightedSelection<TaskType> types = new WeightedSelection<TaskType>();
            // start at 1 to ignore the base task type
            for (int i = 1; i < totalNumTasks; i++) {
                // taskCopies doesn't have a place for the base task
                if (taskCopies[i - 1].CanActivate(totalNumPlayers)) {
                    TaskType type = (TaskType)i;
                    types.AddChoice(type, ConfigManager.instance.GetTaskWeight(type));
                }
            }

            // generate (count) unique tasks
            for (int i = 0; i < count; i++) {
                // as you choose tasks, remove them from the list
                float r = UnityEngine.Random.value;
                results[i] = (int)types.Evaluate(r);
                int index = types.EvaluateToChoiceIndex(r);
                types.RemoveChoice(index);
            }

            return results;
        }

        string GetTaskDescription(int taskType) {
            return taskCopies[taskType - 1].GetDescription();
        }

        public void CreateNotification(int task) {
            for (int i = 0; i < tasksUIObjects.Length; i++) {
                if (currentTasks[i].taskType == task) {
                    TaskType taskType = (TaskType)task;

                    GameObject go = new GameObject($"{taskType:g} completed notification");
                    StartCoroutine(DestroyLater(go, 6));
                    Notification n = go.AddComponent<Notification>();
                    n.enabled = false;
                    // default scale is 1.3
                    n.RootObject.transform.localScale = Vector3.one * 0.67f;
                    n.GetDescription = () => $"You completed {taskType:g}";
                    n.GetTitle = () => $"Task Complete!";
                    n.enabled = true;

                    // 425, 326 + 1.5fx is alright. Long titles might bleed over into the objective panel
                    // new: 435, 105 want: 435, 50. So this needs to be 435, 326-55 = 270
                    n.GenericNotification.transform.localPosition = new Vector3(435, 330, 0) + 1.5f * tasksUIObjects[i].transform.localPosition;
                    // might be better to parent it to the task is corresposds with instead of messing with differing coordinate systems

                    // parent it to the task UI
                    // When I do this, get a bunch of nulls
                    // This object doesn't have any UI attached to it.
                    /*
                    Debug.Log($"i: {i} /Length: {tasksUIObjects.Length}");
                    Transform label = tasksUIObjects[i].transform.Find("Label").transform;
                    go.transform.SetParent(label, true);
                    go.transform.localPosition = new Vector3(-270, 20, 0);
                    go.transform.localScale = Vector3.one * 0.6f;
                    */

                    // Find("CanvasGroup").Find("TextArea");
                    Transform cGroup = n.RootObject.transform.Find("CanvasGroup");
                    if (cGroup) {
                        Transform tArea = cGroup.Find("TextArea");
                        if (tArea) {
                            // old: 1.5
                            tArea.transform.localScale = Vector3.one * 1.25f; // embiggen text
                        }
                    }
                    //n.RootObject.transform.Find("CanvasGroup").Find("TextArea").transform.localScale = Vector3.one * 1.5f; // embiggen text

                    break;
                }
            }
        }

        IEnumerator DestroyLater(GameObject g, float time) {
            yield return new WaitForSeconds(time);
            if (g) {
                Destroy(g);
            } else {
                Debug.Log("Destroy later didn't work");
            }
        }

        public static int GetPlayerNumber(CharacterMaster charMaster) {
            //player CharacterMasters
            for (int i = 0; i < playerCharacterMasters.Count; i++) {
                if (playerCharacterMasters[i] == charMaster) {
                    return i;
                }
            }
            //Debug.Log("CharMaster didn't match any players"); // spams a lot in NoJump bc NPCs jump
            return -1;
        }

        public static CharacterMaster GetPlayerCharacterMaster(int playerNum) {
            if (playerNum < 0 || playerNum >= playerCharacterMasters.Count) {
                Debug.Log($"Task plugin.GetPlayerCharacterMaster() out of bound: {playerNum}");
                return playerCharacterMasters[0];
            }
            return playerCharacterMasters[playerNum];
        }

        public static string GetPlayerName(int playerNum) {
            // for use when all players fail a task
            if (playerNum < 0)
                return "FAILURE";
            return GetPlayerCharacterMaster(playerNum).playerCharacterMasterController.GetDisplayName();
        }

        void TaskCompletion(TaskType taskType, int playerNum, string winMessage) {
            Debug.Log("SERVER(" + (NetworkServer.active ? "active" : "not active") + "):" + winMessage);
            //Chat.AddMessage($"<style=cEvent>{winMessage}</style> (Chat.AddMessage)");
            ChatMessage.Send($"<style=cEvent>{winMessage}</style>"); // works for clients
            // server is inactive when you quit the game.
            // some tasks trigger when the stage ends or you quit the stage.
            // do this to not try to give out items for those tasks
            if (!NetworkServer.active)
                return;
            if (playerNum >= 0)
                GiveReward(taskType, playerNum);

            // NetworkingAPI version
            // but this sends to all clients, doesn't it?
            //new TaskCompletionMessage((int)taskType, playerNum).Send(NetworkDestination.Clients);

            new SyncTaskCompletion((int)taskType, GetPlayerName(playerNum), GetPlayerCharacterMaster(playerNum).networkIdentity).Send(NetworkDestination.Clients);

            /*
            // old miniRPC version
            TaskCompletionInfo info = new TaskCompletionInfo((int)taskType, GetPlayerName(playerNum));
            if (playerNum >= 0)
                taskCompletionClient.Invoke(info, NetworkUser.readOnlyInstancesList[playerNum]);
            taskEndedClient.Invoke(info);
            */
        }

        void UpdateTaskProgress(TaskType taskType, float[] progress) {
            // is this why the host doesn't get rival bars?
            if (!NetworkServer.active) {
                // this might not quite work. the server is probably active in the end stats screen, but the UI are all gone
                Debug.Log("Server no longer active. Not gonna update progress");
                return; // didn't have this before. was it supposed to?
                // was this check just for debugging or meant to be a safety?
            }
            if (stageStartTasks is null || progress is null)
                return;
            int taskIndex = 0;
            for (int i = 0; i < stageStartTasks.Length; i++) {
                if (stageStartTasks[i] == (int)taskType) {
                    taskIndex = i;
                    break;
                }
            }
            // do a loop for tele tasks if I ever make those
            for (int i = 0; i < totalNumPlayers; i++) {
                float myProgress = progress[i];
                float rival = 0;
                for (int j = 0; j < totalNumPlayers; j++) {
                    if (j == i)
                        break;
                    if (progress[j] > rival) {
                        rival = progress[j];
                    }
                }
                ProgressInfo p = new ProgressInfo(taskIndex, Mathf.Clamp01(myProgress), Mathf.Clamp01(rival));
                if (NetworkUser.readOnlyInstancesList is null || NetworkUser.readOnlyInstancesList.Count <= i) {
                    Debug.Log($"Null: {NetworkUser.readOnlyInstancesList is null} Count: {NetworkUser.readOnlyInstancesList.Count} <= {i}");
                    return;
                }

                // updateProgressClient.Invoke(p, NetworkUser.readOnlyInstancesList[i]);

                if (NetworkUser.readOnlyInstancesList[i]?.master?.networkIdentity != null) {
                    new SyncUpdateProgress(p.taskIndex, p.GetMyProgress(), p.GetRivalProgress(), NetworkUser.readOnlyInstancesList[i].master.networkIdentity).Send(NetworkDestination.Clients);
                }

                //new UpdateProgressMessage(p, i).Send(NetworkDestination.Clients);
            }
        }

        void GiveReward(TaskType task, int playerNum) {
            // Some tasks end when the level ends.
            if (playerCharacterMasters is null)
                return;
            if (rewards[(int)task].type == RewardType.Item) {
                //PickupCatalog.GetPickupDef(rewards[rewardIndex].item).itemIndex
                // old version
                //playerCharacterMasters[playerNum].inventory.GiveItem(rewards[(int)task].item.itemIndex, rewards[(int)task].numItems);
                playerCharacterMasters[playerNum].inventory.GiveItem(PickupCatalog.GetPickupDef(rewards[(int)task].item).itemIndex, rewards[(int)task].numItems);
                //PickupCatalog.GetPickupDef(rewards[(int)task].item).itemIndex

                // show text in chat
                PickupDef pickDef = PickupCatalog.GetPickupDef(rewards[(int)task].item);

                // i think it uses your current num of that item, it doesn't care how many you get at once so you don't have to pass it in
                typeof(GenericPickupController).InvokeMethod("SendPickupMessage", GetPlayerCharacterMaster(playerNum), rewards[(int)task].item);// (CharacterMaster master, PickupIndex pickupIndex)

            } else if (rewards[(int)task].type == RewardType.TempItem) {
                // old version
                //playerCharacterMasters[playerNum].inventory.GiveItem(rewards[(int)task].item.itemIndex, rewards[(int)task].numItems);
                playerCharacterMasters[playerNum].inventory.GiveItem(PickupCatalog.GetPickupDef(rewards[(int)task].item).itemIndex, rewards[(int)task].numItems);

                PickupDef pickDef = PickupCatalog.GetPickupDef(rewards[(int)task].item);

                typeof(GenericPickupController).InvokeMethod("SendPickupMessage", GetPlayerCharacterMaster(playerNum), rewards[(int)task].item);

                // remove these items later
                // Record what items to remove
                RecordTempItems(playerNum, rewards[(int)task].item, rewards[(int)task].numItems);
            } else if (rewards[(int)task].type == RewardType.Command) {

                // run only if command is not active, so it isn't turned off at the end
                if (!CommandArtifactManager.IsCommandArtifactEnabled) {
                    RunArtifactManager.instance.SetArtifactEnabled(RoR2Content.Artifacts.commandArtifactDef, true);
                    CommandArtifactManager.OnArtifactEnabled(RunArtifactManager.instance, RoR2Content.Artifacts.commandArtifactDef);
                    if (CommandArtifactManager.commandCubePrefab is null) {
                        Debug.LogWarning("CommandCubePefab is null!");
                        CommandArtifactManager.commandCubePrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Command.CommandCube_prefab).WaitForCompletion();
                        //Chat.AddMessage("commandCubePrefab is null"); // always null??? Is GetField wrong? GetMember GetProperty GetFieldCached???
                    }

                    //icon.image.texture = RoR2.LegacyResourcesAPI.Load<Texture>(path);// Resources.Load<Texture>(path);
                    //typeof(CommandArtifactManager).SetFieldValue<GameObject>("commandCubePrefab", Resources.Load<GameObject>("Prefabs/NetworkedObjects/CommandCube"));
                    // typeof(CommandArtifactManager).SetFieldValue<GameObject>("commandCubePrefab", RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/CommandCube"));

                    // need to make an item droplet. Specifically have it hit the ground
                    On.RoR2.PickupDropletController.OnCollisionEnter += TurnOffCommand;
                    // technically, the next droplet to hit the ground is the command droplet. Whether it was created right here or if it was in the air already when this one was spawned.
                }

                // create a droplet
                PickupIndex p = rewards[(int)task].item;
                PickupDropletController.CreatePickupDroplet(p, GetPlayerCharacterMaster(playerNum).GetBody().transform.position, GetPlayerCharacterMaster(playerNum).GetBody().transform.forward * 5);
            } else if (rewards[(int)task].type == RewardType.Drone) {
                RewardBuilder.GivePlayerDrone(playerNum, rewards[(int)task].dronePath);
            } else {
                // give gold or xp
                //playerCharacterMasters[playerNum].inventory
            }
        }

        static void TurnOffCommand(On.RoR2.PickupDropletController.orig_OnCollisionEnter orig, PickupDropletController self, Collision collision) {
            orig(self, collision);
            if (NetworkServer.active) {
                RunArtifactManager.instance.SetArtifactEnabled(RoR2Content.Artifacts.commandArtifactDef, false);
                CommandArtifactManager.OnArtifactDisabled(RunArtifactManager.instance, RoR2Content.Artifacts.commandArtifactDef);
            }
            On.RoR2.PickupDropletController.OnCollisionEnter -= TurnOffCommand; // icky
        }

        void RecordTempItems(int playerNum, PickupIndex item, int count) {
            if (playerNum < 0) {
                Debug.Log("Didn't find a match. Couldn't record items");
                return;
            }
            TempItemLists[playerNum].Add(new TempItem(item, count));
        }

        void RemoveTempItems() {
            for (int i = 0; i < playerCharacterMasters.Count; i++) {
                List<TempItem> list = TempItemLists[i];
                int count = 0;
                int partials = 0; // temp items partially removed

                while (list.Count > 0) {
                    count++;
                    if (count > 50) {
                        Debug.Log("Oops. Infinite loop. Quitting remove temp items");
                        return;
                    }

                    if (list.Count <= partials)
                        break;
                    TempItem temp = list[0 + partials];

                    int toRemove = Math.Min(temp.count, 3); // remove 3/5 unless they have less than 3
                    temp.count -= toRemove;
                    TempItemLists[i][0 + partials] = temp;

                    // maybe I should do a chat message like when someone picks up an item
                    // don't really want to spam chat. Multiple players might be losing multiple different items.
                    // could be local so you only see your own losses. That would be more work. This code runs on the server, not locally.
                    Debug.Log($"Removing {toRemove} {temp.item:g}");
                    // old version
                    //playerCharacterMasters[i].inventory.RemoveItem(temp.item.itemIndex, toRemove);
                    playerCharacterMasters[i].inventory.RemoveItem(PickupCatalog.GetPickupDef(temp.item).itemIndex, toRemove);
                    //PickupCatalog.GetPickupDef(temp.item).itemIndex
                    //PickupCatalog.GetPickupDef(temp.item).itemIndex
                    if (temp.count <= 0) {
                        list.RemoveAt(0);
                    } else {
                        partials++;
                    }
                }
            }
        }

        public static void StartPreonEvent() {
            instance.preonEventEqCache = new EquipmentIndex[playerCharacterMasters.Count];
            for (int i = 0; i < playerCharacterMasters.Count; i++) {
                instance.preonEventEqCache[i] = playerCharacterMasters[i].inventory.currentEquipmentIndex;
                playerCharacterMasters[i].inventory.SetEquipmentIndex(RoR2Content.Equipment.BFG.equipmentIndex);
                //playerCharacterMasters[i].inventory.GiveItem(ItemIndex.AutoCastEquipment); // annoying while testing stuff
                playerCharacterMasters[i].inventory.GiveItem(RoR2Content.Items.Talisman); // soulbound catalyst. Kills reduce eq cd
                playerCharacterMasters[i].inventory.GiveItem(RoR2Content.Items.EquipmentMagazine, 5);
            }

        }

        public static void EndPreonEvent() {
            for (int i = 0; i < playerCharacterMasters.Count; i++) {
                playerCharacterMasters[i].inventory.SetEquipmentIndex(instance.preonEventEqCache[i]);
                //playerCharacterMasters[i].inventory.RemoveItem(ItemIndex.AutoCastEquipment);
                playerCharacterMasters[i].inventory.RemoveItem(RoR2Content.Items.Talisman);
                playerCharacterMasters[i].inventory.RemoveItem(RoR2Content.Items.EquipmentMagazine, 5);
            }
        }

        public static void StartLockboxTask() {
            // give everyone a key
            // task is only available when someone gets the first key
            // but now you need a key to open the box
            // don't think I need to remove them after. 
            // It could get out of hand if you get multiple find the lockbox tasks
            for (int i = 0; i < playerCharacterMasters.Count; i++) {
                playerCharacterMasters[i].inventory.GiveItem(RoR2Content.Items.TreasureCache);
            }
        }
    }

    public class SyncTaskCompletion : INetMessage {

        public int taskType;
        public string winnerName;
        NetworkIdentity netIdentity;

        public SyncTaskCompletion() {
        }

        public SyncTaskCompletion(int _taskType, string _winnerName, NetworkIdentity _netIdentity) {
            taskType = _taskType;
            winnerName = _winnerName;
            netIdentity = _netIdentity;
        }

        public void Serialize(NetworkWriter writer) {
            writer.Write(taskType);
            writer.Write(winnerName);
            writer.Write(netIdentity);
        }

        public void Deserialize(NetworkReader reader) {
            taskType = reader.ReadInt32();
            winnerName = reader.ReadString();
            netIdentity = reader.ReadNetworkIdentity();
        }

        public void OnReceived() {
            if (!Util.HasEffectiveAuthority(netIdentity)) {
                return;
            }
            // code that runs on the client
            // user specifies which user so I don't have to check
            TasksPlugin.instance.CreateNotification(taskType);
            TasksPlugin.instance.InvokeOnPopup(taskType);
            // winner has this called and the basic one
            // so don't need to pass the name here and create 2 text fields
            TasksPlugin.instance.RemoveObjectivePanel(taskType); //, taskCompletionInfo.winnerName
        }
    }

    public class SyncTaskEnded : INetMessage {

        public int taskType;
        public string winnerName;

        public SyncTaskEnded() {
        }

        public SyncTaskEnded(int _taskType, string _winnerName) {
            taskType = _taskType;
            winnerName = _winnerName;
        }

        public void Serialize(NetworkWriter writer) {
            writer.Write(taskType);
            writer.Write(winnerName);
        }

        public void Deserialize(NetworkReader reader) {
            taskType = reader.ReadInt32();
            winnerName = reader.ReadString();
        }

        public void OnReceived() {
            TasksPlugin.instance.RemoveObjectivePanel(taskType, winnerName);
        }
    }

    public class SyncUpdateTask : INetMessage {

        public int taskType;
        public string description;
        public bool completed;
        public int index;
        public int total;
        public Reward reward;

        public SyncUpdateTask() {
        }

        public SyncUpdateTask(int _type, string _description, bool _completed, int _index, int _total, Reward _reward) {
            taskType = _type;
            description = _description;
            completed = _completed;
            index = _index;
            total = _total;
            reward = _reward;
        }

        public void Serialize(NetworkWriter writer) {
            writer.Write(taskType);
            writer.Write(description);
            writer.Write(completed);
            writer.Write(index);
            writer.Write(total);

            writer.Write((int)reward.type);
            writer.Write(reward.item.value);
            writer.Write(reward.numItems);
            writer.Write(reward.temporary);
            writer.Write(reward.gold);
            writer.Write(reward.xp);
            writer.Write(reward.dronePath);
        }

        public void Deserialize(NetworkReader reader) {
            taskType = reader.ReadInt32();
            description = reader.ReadString();
            completed = reader.ReadBoolean();
            index = reader.ReadInt32();
            total = reader.ReadInt32();

            int type = reader.ReadInt32();
            int item = reader.ReadInt32();
            int numItems = reader.ReadInt32();
            bool temp = reader.ReadBoolean();
            int gold = reader.ReadInt32();
            int xp = reader.ReadInt32();
            string path = reader.ReadString();

            PickupIndex p = new PickupIndex(item);
            reward = new Reward((RewardType)type, p, numItems, temp, gold, xp, path);
        }

        public void OnReceived() {
            // this is called once for each task when tasks are created
            if (TasksPlugin.instance.currentTasks is null || TasksPlugin.instance.currentTasks.Length != total) {
                TasksPlugin.instance.currentTasks = new TaskInfo[total];
            }

            // make sure your number of UI elements matches the host's taskInfo they are giving you
            // the number of tasks is determined by the config file which can differ player-to-player
            if (total != TasksPlugin.instance.tasksUIObjects.Length) {
                TasksPlugin.instance.tasksUIObjects = new GameObject[total];
                TasksPlugin.instance.tasksUIRects = new RectTransform[total];
                TasksPlugin.instance.rivalTasksUIRects = new RectTransform[total];
            }

            TasksPlugin.instance.currentTasks[index] = new TaskInfo(taskType, description, completed, index, total, reward);

            TasksPlugin.instance.rewards[(int)taskType] = reward;

            // make each UI element one at a time for each task
            TasksPlugin.instance.UpdateTasksUI(index);
        }
    }

    public class SyncUpdateProgress : INetMessage {

        public int taskIndex;
        int myProgress;
        int rivalProgress;
        ProgressInfo progressInfo;
        NetworkIdentity netIdentity;

        public SyncUpdateProgress() {
        }

        public SyncUpdateProgress(int _taskIndex, float _myProgress, float _rivalProgress, NetworkIdentity _netIdentity) {
            taskIndex = _taskIndex;
            // can't serialize floats (or I don't know how)
            // so just convert from 0.0->1.0 to 00 to 10
            // Lose most decimal places, but didn't need them anyway
            myProgress = (int)(_myProgress * 10);
            rivalProgress = (int)(_rivalProgress * 10);
            netIdentity = _netIdentity;

        }

        public void Serialize(NetworkWriter writer) {
            writer.Write(taskIndex);
            writer.Write(myProgress);
            writer.Write(rivalProgress);
            writer.Write(netIdentity);
        }

        public void Deserialize(NetworkReader reader) {
            taskIndex = reader.ReadInt32();
            myProgress = reader.ReadInt32();
            rivalProgress = reader.ReadInt32();
            netIdentity = reader.ReadNetworkIdentity();
        }

        public void OnReceived() {
            if (!Util.HasEffectiveAuthority(netIdentity)) {
                return;
            }

            progressInfo = new ProgressInfo(taskIndex, myProgress, rivalProgress);

            bool playerLeading = progressInfo.GetMyProgress() > progressInfo.GetRivalProgress();
            TasksPlugin.instance.UpdateProgress(TasksPlugin.instance.tasksUIRects[progressInfo.taskIndex], progressInfo.GetMyProgress());
            TasksPlugin.instance.UpdateProgress(TasksPlugin.instance.rivalTasksUIRects[progressInfo.taskIndex], progressInfo.GetRivalProgress(), playerLeading);
        }
    }
}
