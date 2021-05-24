using System;
using BepInEx;
using RoR2;
using RoR2.UI;
using RoR2.Artifacts;
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
using UnityEngine.UI;
using R2API.Networking;
using R2API.Networking.Interfaces;

// Networking stuff. Tutorials and examples
// https://github.com/risk-of-thunder/R2Wiki/wiki/Networking-&-Multiplayer-mods-(MiniRPCLib)



namespace Tasks
{
    [BepInDependency("com.bepis.r2api")]
    [BepInDependency(MiniRpcPlugin.Dependency)]
    [R2APISubmoduleDependency(nameof(ItemDropAPI), nameof(NetworkingAPI))]
    //[R2APISubmoduleDependency(nameof(yourDesiredAPI))]
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public sealed class TasksPlugin : BaseUnityPlugin
    {
        public const string
            MODNAME = "Tasks",
            AUTHOR = "Solrun",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "0.0.0";

        public static TasksPlugin instance;

        public static event Action<int, int> OnActivate;
        public static event Action<int> OnDeactivate;
        public static event Action OnResetAll;
        public static event Action<int> OnPopup;
        public static event Action<int, SkillSlot> OnAbilityUsed;

        public IRpcAction<int> taskCompletionClient { get; set; }
        public IRpcAction<int> taskEndedClient { get; set; }
        public IRpcAction<TaskInfo> updateTaskClient { get; set; }
        public IRpcAction<ProgressInfo> updateProgressClient { get; set; }

        
        static List<CharacterMaster> playerCharacterMasters;
        int totalNumPlayers = 0;

        int totalNumTasks;
        Reward[] rewards;
        List<TempItem>[] TempItemLists;
        EquipmentIndex[] preonEventEqCache; // where your equipment is stored when the preon event starts and swaps it for a preon

        // Server
        int[] stageStartTasks;
        int[] teleStartTasks;
        Task[] taskCopies;
        bool telePlaced = false;

        // Client
        TaskInfo[] currentTasks;
        GameObject[] tasksUIObjects;
        RectTransform[] tasksUIRects;
        RectTransform[] rivalTasksUIRects; // shows the leader or next nearest if you are the leader

        HUD hud;
        ObjectivePanelController panel;
        GameObject itemIconPrefabCopy;


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Awake is automatically called by Unity")]
        private void Awake() //Called when loaded by BepInEx.
        {
            Chat.AddMessage("Loaded Task plugin");

            if(instance is null)
            {
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

        public void Update()
        {
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
            if(Input.GetKeyDown(KeyCode.F4))
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
            if(Input.GetKeyDown(KeyCode.F5))
            {
                // I can call this on the server to show on all players
                ChatMessage.Send("Do I need to add this API? No. This should go to each player");
            }
            if(Input.GetKeyDown(KeyCode.F6))
            {
                Chat.AddMessage("Pressed F6");
                // creating a command orb
                //typeof(GenericPickupController).InvokeMethod("SendPickupMessage", GetPlayerCharacterMaster(playerNum), rewards[(int)task].item);
                // turn on command I believe
                
                typeof(CommandArtifactManager).InvokeMethod("OnArtifactEnabled", RunArtifactManager.instance, RoR2Content.Artifacts.commandArtifactDef);
                if(typeof(CommandArtifactManager).GetField("commandCubePrefab") is null)
                {
                    Chat.AddMessage("commandCubePrefab is null");
                }
                typeof(CommandArtifactManager).SetFieldValue<GameObject>("commandCubePrefab",  Resources.Load<GameObject>("Prefabs/NetworkedObjects/CommandCube"));
                // need to make an item droplet. Specifically have it hit the ground
                PickupDropletController.onDropletHitGroundServer += TurnOffCommand;
                // technically, the next droplet to hit the ground is the command droplet. Whether it was created right here or if it was in the air already when this one was spawned.

                // icon
                //RoR2Content.Artifacts.commandArtifactDef.smallIconSelectedSprite

                // create a droplet
                PickupIndex p = new PickupIndex(RoR2Content.Items.BarrierOnKill.itemIndex);
                PickupDropletController.CreatePickupDroplet(p, GetPlayerCharacterMaster(0).GetBody().transform.position, GetPlayerCharacterMaster(0).GetBody().transform.forward);
            }
            if(Input.GetKeyDown(KeyCode.F7))
            {
                Chat.AddMessage("Pressed F7");

            }

        }

        void GameSetup(Run run)
        {
            // clients need these
            totalNumTasks = Enum.GetNames(typeof(TaskType)).Length;
            rewards = new Reward[totalNumTasks];

            Debug.Log($"Number of players: {run.participatingPlayerCount} Living Players: {run.livingPlayerCount}");
            totalNumPlayers = run.participatingPlayerCount;
            playerCharacterMasters = new List<CharacterMaster>(totalNumPlayers);

            PopulatePlayerCharaterMasterList();

            if (!NetworkServer.active)
            {
                // this is the client
                return;
            }

            TaskSetup();
            
            PopulateTempItemLists();

            Debug.Log("ItemInventoryDisplay");
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

            // -1 forces it to match for deactivation purposes
            // if(id == myId || id < 0)
            // Means I don't have to iterate over every task id and check if it matches n*n times
            OnDeactivate?.Invoke(-1);
        }

        void TaskSetup()
        {
            // How to make a new task
            // make the class
            // increment the size of the array
            // Create a new object, add it to the array
            // add a new type to the TaskType enum in Task.cs
            // update the type in the class you made
            // Update ConfigManager with the new task (kinda optional. Won't break if you don't)
            
            Debug.Log("Creating Task Objects");
            taskCopies = new Task[25];

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





            // Can I do something like this?
            // From RoR2.Chat.ChatMessageBase.BuildMessageTypeNetMap()
            // foreach (Type type in typeof(Chat.ChatMessageBase).Assembly.GetTypes())
            //{
            //    if (type.IsSubclassOf(typeof(Chat.ChatMessageBase)))
        }

        void PopulatePlayerCharaterMasterList()
        {
            for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
            {
                if (CharacterMaster.readOnlyInstancesList[i].playerCharacterMasterController != null)
                {
                    //characterMasters
                    playerCharacterMasters.Add(CharacterMaster.readOnlyInstancesList[i]);
                }
            }
        }

        void PopulateTempItemLists()
        {
            TempItemLists = new List<TempItem>[CharacterMaster.readOnlyInstancesList.Count];

            for (int i = 0; i < TempItemLists.Length; i++)
            {
                TempItemLists[i] = new List<TempItem>();
            }
        }

        void SetupNetworking()
        {
            // new R2API networking API
            NetworkingAPI.RegisterMessageType<SetupTaskMessage>();
            NetworkingAPI.RegisterMessageType<TaskCompletionMessage>();
            NetworkingAPI.RegisterMessageType<UpdateProgressMessage>();

            
            // Old miniRpc
            var miniRpc = MiniRpc.CreateInstance(GUID);
            taskCompletionClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, int task) =>
            {
                // code that runs on the client
                // user specifies which user so I don't have to check
                CreateNotification(task);
                OnPopup?.Invoke(task);
                RemoveObjectivePanel(task);
            });

            taskEndedClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, int task) =>
            {
                // task ended
                RemoveObjectivePanel(task);
            });

            updateTaskClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, TaskInfo taskInfo) =>
            {
                if (currentTasks is null || currentTasks.Length != taskInfo.total)
                {
                    currentTasks = new TaskInfo[taskInfo.total];
                }

                currentTasks[taskInfo.index] = taskInfo;
                rewards[(int)taskInfo.taskType] = taskInfo.reward;

                UpdateTasksUI(taskInfo.index);
            });

            updateProgressClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, ProgressInfo progressInfo) =>
            {
                bool playerLeading = progressInfo.GetMyProgress() > progressInfo.GetRivalProgress();
                UpdateProgress(tasksUIRects[progressInfo.taskIndex], progressInfo.GetMyProgress());
                UpdateProgress(rivalTasksUIRects[progressInfo.taskIndex], progressInfo.GetRivalProgress(), playerLeading);
            });
            
        }

        void SetGameHooks()
        {
            // Used for tasks
            On.RoR2.CharacterBody.OnSkillActivated += (orig, self, param) =>
            {
                // triggers on npcs attacking too. Everything in the game is a skill
                // only runs if the skill is off cd
                // But it also triggers on skills you can cancel. Like engi turrets or missiles. You press R, the UI shows up
                // but then you cancel the build and the skill doesn't go on CD
                // I'm not sure if there's a general solution for that
                // only run this stuff on the server
                if (NetworkServer.active)
                {
                    if (self.isPlayerControlled)
                    {
                        int i = GetPlayerNumber(self.master);
                        SkillSlot skill = self.skillLocator.FindSkillSlot(param);
                        OnAbilityUsed?.Invoke(i, skill);
                    }
                }

                orig(self, param);
            };
            GlobalEventManager.OnInteractionsGlobal += (Interactor interactor, IInteractable interactable, GameObject go) =>
            {
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
                // when you use a 3D printer or eq drone i think or pool prob
                // PurchaseInteraction.onItemSpentOnPurchase += method
                // purchaseInteraction.gameObject.name.Contains("Duplicator")

                // it gets called when you start the tp event and again when you interact with the tp to leave
                if (go?.GetComponent<TeleporterInteraction>())
                {
                    // this might be true for interacting with the end tp to switch to loop mode
                    Debug.Log("Interacted with TP. InterType: " + interactableType + " name: " + go.name);
                }
                else if(go?.GetComponent<SceneExitController>())
                {
                    // not a tp so probably some kind of tp like blue or gold
                    Debug.Log($"Interacted with a SceneExitController {go.name}");
                    StageEnd();
                }
                
            };
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
            TeleporterInteraction.onTeleporterFinishGlobal += (_) =>
            {
                // Runs when you click the tele to move to the next stage (after you kill the boss and charge the tele)
                // triggers on the client
                Debug.Log("TP finished and player chose to leave");
                if (NetworkServer.active)
                {
                    StageEnd();
                }
            };

            BossGroup.onBossGroupDefeatedServer += (BossGroup group) =>
            {
                // this works. Timer too
                //Chat.AddMessage($"Boss defeated in {group.fixedTimeSinceEnabled} seconds");
            };

            On.RoR2.Run.OnServerTeleporterPlaced += (orig, self, director, teleporter) =>
            {
                orig(self, director, teleporter);
                //Chat.AddMessage("Placed the TP"); // shouldn't run on stages without a tp
                // this should run before HUD.Awake
                // Have to wait til HUD.Awake so stuff isn't null
                telePlaced = true;
                // telePlaced = false at end of stage to reset for the next stage
            };

            // controls starting tasks
            On.RoR2.UI.HUD.Awake += (orig, self) =>
            {
                // Have to remember to activate the spawned GO
                // This also gets called at the start of each stage. 
                // GenerateTasks waits 3 seconds and that seems to do it
                orig(self);
                hud = self;
                panel = self.objectivePanelController;
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

                if (telePlaced)
                {
                    if (NetworkServer.active)
                        GenerateTasks(numberOfStageTasks);
                }
            };
        }

        // These 3 ___CLient() methods are used by NetworkingAPI
        // but aren't being used ATM bc I got miniRPC to work and I know how to send messages to specific players with that
        public void SetupTasksClient(TaskInfo taskInfo)
        {
            //Debug.Log("SetupTasksClient");
            if (currentTasks is null || currentTasks.Length != taskInfo.total)
            {
                currentTasks = new TaskInfo[taskInfo.total];
            }

            currentTasks[taskInfo.index] = taskInfo;
            rewards[(int)taskInfo.taskType] = taskInfo.reward;

            UpdateTasksUI(taskInfo.index);
        }

        public void TaskCompletionClient(int task, int playerNum)
        {
            // am I the player?
            // instances.master should be the local player
            // GetPlayer(num) should be the list of all players. Is the list in the same order for each client???
            bool isWinner = PlayerCharacterMasterController.instances[0].master == GetPlayerCharacterMaster(playerNum); // Does not work
            // [Info   : Unity Log] Task 8 complete by player 1. Is it me? False
            // should be true. Does that mean the player order is different?
            // Should I send out the player order?
            // or other info like netID?
            Debug.Log($"Player {playerNum} won. I think I am player {GetPlayerNumber(PlayerCharacterMasterController.instances[0].master)}");
            Debug.Log($"Task {(TaskType)task:g} complete by player {playerNum}. Is it me? {isWinner}");
            if (isWinner)
            {
                CreateNotification(task);
                OnPopup?.Invoke(task);
            }
            RemoveObjectivePanel(task);
        }

        public void UpdateProgressClient(ProgressInfo progressInfo, int playerNum)
        {
            bool isMe = PlayerCharacterMasterController.instances[0].master == GetPlayerCharacterMaster(playerNum); // doesn't work
            if (isMe)
            {
                bool playerLeading = progressInfo.GetMyProgress() > progressInfo.GetRivalProgress();
                UpdateProgress(tasksUIRects[progressInfo.taskIndex], progressInfo.GetMyProgress());
                UpdateProgress(rivalTasksUIRects[progressInfo.taskIndex], progressInfo.GetRivalProgress(), playerLeading);
            }
        }

        void UpdateTasksUI(int taskIndex, string text = "")
        {
            if (tasksUIObjects[taskIndex] is null)
            {
                tasksUIObjects[taskIndex] = Instantiate(panel.objectiveTrackerPrefab, hud.objectivePanelController.transform);
                // rewards is totalNumTasks long
                // taskIndex is like 6 at most.
                int rewardIndex = currentTasks[taskIndex].taskType;

                tasksUIObjects[taskIndex].SetActive(true);

                if (itemIconPrefabCopy is null)
                {
                    itemIconPrefabCopy = FindObjectOfType<ItemInventoryDisplay>().itemIconPrefab;
                }

                ItemIcon icon = Instantiate(itemIconPrefabCopy, tasksUIObjects[taskIndex].transform).GetComponent<ItemIcon>();
                icon.SetItemIndex(rewards[rewardIndex].item.itemIndex, rewards[rewardIndex].numItems);

                RectTransform rect = icon.rectTransform;
                rect.localScale = Vector3.one * 0.5f;

                Image checkBox = tasksUIObjects[taskIndex].transform.Find("Checkbox").GetComponent<Image>();
                checkBox.color = new Color(0, 0, 0, 0); // invisible

                if (rewards[rewardIndex].type == RewardType.Command)
                {
                    checkBox.sprite = RoR2Content.Artifacts.commandArtifactDef.smallIconSelectedSprite;
                    checkBox.color = Color.white;

                    RectTransform checkRect = checkBox.rectTransform;
                    // the smaller the scale, the more to the left it is
                    checkRect.localScale = Vector3.one * 0.6f;
                    checkRect.localPosition += new Vector3(-5, -10, 0);
                    checkRect.SetAsLastSibling();
                }
                else if (rewards[rewardIndex].type == RewardType.Drone)
                {
                    string path = RewardBuilder.GetDroneTexturePath(rewards[rewardIndex].dronePath);
                    icon.image.texture = Resources.Load<Texture>(path);
                }
            }
            tasksUIObjects[taskIndex].SetActive(true);

            TMPro.TextMeshProUGUI textMeshLabel = tasksUIObjects[taskIndex].transform.Find("Label").GetComponent<TMPro.TextMeshProUGUI>();
            if (textMeshLabel != null)
            {
                textMeshLabel.text = (text == "") ? currentTasks[taskIndex].description : text;
            }

            tasksUIRects[taskIndex] = CreateTaskProgressBar(textMeshLabel.transform);
            rivalTasksUIRects[taskIndex] = CreateTaskProgressBar(textMeshLabel.transform);
        }

        RectTransform CreateTaskProgressBar(Transform parent)
        {
            GameObject proBar = new GameObject("ProBar");
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

        /// <summary>
        /// The rect to update and the percent complete (from 0 to 1)
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="percent01">Percent complete between 0 and 1</param>
        void UpdateProgress(RectTransform rect, float percent01)
        {
            if(rect is null || rect.sizeDelta == null)
            {
                Debug.Log("Rect was null");
                return;
            }
            // rect can be non null, but then sizeDelta is null??
            // not sure if rect.size == null will help or if that will just push the issue elsewhere
            // still have the error with it
            // the null is triggered when quitting the game. And new games seem to work so it's low priority
            // correction: unhooks get broken
            rect.sizeDelta = new Vector2(percent01 * 113, 3); // 113 just looks about right
        }

        void UpdateProgress(RectTransform rect, float percent01, bool playerLeading)
        {
            if (rect is null)
            {
                Debug.Log("Rect was null in rival");
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
            if (playerLeading)
            {
                image.color = new Color(234 / 255f, 229 / 255f, 135 / 255f); // barrier gold (the outline, the colour over the hp bar is see through so it's a little green)
                rect.SetAsLastSibling(); // put on top
                percent01 = Math.Max(0, percent01 - 0.02f); // might help with visuals when nearly tied
            }
            else
            {
                image.color = new Color(68 / 255f, 94 / 255f, 181 / 255f); // shield blue
                rect.SetAsFirstSibling(); // put below
                if (percent01 > 0)
                {
                    percent01 = Mathf.Min(1, percent01 + 0.02f); // should help visuals when exactly tied
                }
            }
            UpdateProgress(rect, percent01);
        }

        void RemoveObjectivePanel(int taskType)
        {
            for (int i = 0; i < tasksUIObjects.Length; i++)
            {
                if(currentTasks[i].taskType == taskType)
                {
                    if(tasksUIObjects[i] is null)
                    {
                        Debug.Log($"UI Object {i} was null");
                        break;
                    }

                    TMPro.TextMeshProUGUI textMeshLabel = tasksUIObjects[i].transform.Find("Label").GetComponent<TMPro.TextMeshProUGUI>();
                    if(textMeshLabel is null)
                    {
                        Debug.Log("Couldn't strikethrough");
                    }
                    else
                    {
                        textMeshLabel.color = Color.grey;
                        textMeshLabel.fontStyle |= TMPro.FontStyles.Strikethrough;

                        ItemIcon icon = tasksUIObjects[i].GetComponentInChildren<ItemIcon>();
                        if(icon)
                            icon.image.color = Color.grey;
                        Image checkBox = tasksUIObjects[i].transform.Find("Checkbox")?.GetComponent<Image>();
                        if (checkBox)
                        {
                            checkBox.color = new Color(0.5f, 0.5f, 0.5f, checkBox.color.a); // grey, but keeping the alpha. The checkbox is usually invisible. This keeps that
                        }

                    }
                }
            }
        }

        void GenerateTasks(int numTasks)
        {
            StartCoroutine(StartTasksWorkaround(numTasks));
        }

        IEnumerator StartTasksWorkaround(int numTasks)
        {
            // If I start tasks right at the beginning, the player's body is null
            yield return new WaitForSeconds(3); // 3 sec is arbitrary, but seems to work. Could fail if stages later in the run take longer to load maybe
            StartTasks(numTasks);
        }

        void StartTasks(int numTasks, bool teleTasks = false)
        {
            int[] taskIDNumbers = GetWeightedTasks(numTasks);// GetRandomUniqueTasks(numTasks);

            if (teleTasks)
            {
                teleStartTasks = taskIDNumbers;
            }
            else
            {
                stageStartTasks = taskIDNumbers;
            }

            for (int i = 0; i < taskIDNumbers.Length; i++)
            {
                // rewards[TaskType.DamageInTime]
                // array is as large as TaskType enum (-1)
                // but not necessarily full. Maybe only tasks 3, 5, 8 are active
                rewards[taskIDNumbers[i]] = RewardBuilder.CreateRandomReward();

                // These 2 lines for debugging
                int r = taskIDNumbers[i];
                Debug.Log(String.Format("Task: {0}. Reward: {1} From r: {2}", ((TaskType)r).ToString(), rewards[r].ToString(), r));

                OnActivate?.Invoke(taskIDNumbers[i], totalNumPlayers);
            }

            for (int i = 0; i < totalNumPlayers; i++)
            {

                if(NetworkUser.readOnlyInstancesList is null)
                {
                    Debug.Log("List is null");
                    return;
                }
                // Send a list of all tasks to all players
                if (NetworkUser.readOnlyInstancesList.Count > 0)
                {
                    for (int j = 0; j < taskIDNumbers.Length; j++)
                    {
                        TaskInfo info = new TaskInfo(taskIDNumbers[j], GetTaskDescription(taskIDNumbers[j]), false, j, taskIDNumbers.Length, rewards[taskIDNumbers[j]]);
                        updateTaskClient?.Invoke(info, NetworkUser.readOnlyInstancesList[i]);
                        //new SetupTaskMessage(info).Send(NetworkDestination.Clients);
                    }
                }
                else
                {
                    Debug.Log("No network users");
                }
                //break; // testing NEtworkingAPI. I think it sends to all clients at once, not one at a time so don't need the loop
            }
        }

        void StageEnd()
        {
            // Do this before ending all tasks
            // some tasks are only finished when the stage ends (deal the most damage, etc)
            // So they need to give their reward after temp items are removed in case they give temp items
            // if there was no tele, there were no tasks
            if (telePlaced)
            {
                RemoveTempItems();
                EndAllTasks();
            }
            telePlaced = false;
        }

        void EndAllTasks()
        {
            if (stageStartTasks != null)
            {
                for (int i = 0; i < stageStartTasks.Length; i++)
                {
                    OnDeactivate?.Invoke(stageStartTasks[i]);
                    RemoveObjectivePanel(stageStartTasks[i]);
                }
            }
            if (teleStartTasks != null)
            {
                for (int i = 0; i < teleStartTasks.Length; i++)
                {
                    OnDeactivate?.Invoke(teleStartTasks[i]);
                    RemoveObjectivePanel(teleStartTasks[i]);
                }
            }
        }

        int[] GetWeightedTasks(int count)
        {
            int[] results = new int[count];
            WeightedSelection<TaskType> types = new WeightedSelection<TaskType>();
            // start at 1 to ignore the base task type
            for (int i = 1; i < totalNumTasks; i++)
            {
                // taskCopies doesn't have a place for the base task
                if (taskCopies[i - 1].CanActivate(totalNumPlayers))
                {
                    TaskType type = (TaskType)i;
                    types.AddChoice(type, ConfigManager.instance.GetTaskWeight(type));
                }
            }

            // generate (count) unique tasks
            for (int i = 0; i < count; i++)
            {
                // as you choose tasks, remove them from the list
                float r = UnityEngine.Random.value;
                results[i] = (int)types.Evaluate(r);
                int index = types.EvaluteToChoiceIndex(r);
                types.RemoveChoice(index);
            }

            return results;
        }

        string GetTaskDescription(int taskType)
        {
            return taskCopies[taskType - 1].GetDescription();
        }

        void CreateNotification(int task)
        {
            for (int i = 0; i < tasksUIObjects.Length; i++)
            {
                if (currentTasks[i].taskType == task)
                {
                    TaskType taskType = (TaskType)task;

                    GameObject go = new GameObject($"{taskType:g} completed notification");
                    Notification n = go.AddComponent<Notification>();
                    n.enabled = false;
                    // default scale is 1.3
                    n.RootObject.transform.localScale = Vector3.one * 0.67f;
                    n.GetDescription = () => $"You completed {taskType:g}";
                    n.GetTitle = () => $"Task Complete!";
                    n.enabled = true;

                    // 425, 326 + 1.5fx is alright. Long titles might bleed over into the objective panel
                    n.GenericNotification.transform.localPosition = new Vector3(435, 326, 0) + 1.5f * tasksUIObjects[i].transform.localPosition;
                    n.RootObject.transform.Find("TextArea").transform.localScale = Vector3.one * 1.5f; // embiggen text

                    break;
                }
            }
        }

        public static int GetPlayerNumber(CharacterMaster charMaster)
        {
            //player CharacterMasters
            for (int i = 0; i < playerCharacterMasters.Count; i++)
            {
                if(playerCharacterMasters[i] == charMaster)
                {
                    return i;
                }
            }
            Debug.Log("CharMaster didn't match any players");
            return -1;
        }

        public static CharacterMaster GetPlayerCharacterMaster(int playerNum)
        {
            return playerCharacterMasters[playerNum];
        }

        void TaskCompletion(TaskType taskType, int playerNum)
        {
            Debug.Log("SERVER(" + (NetworkServer.active ? "active" : "not active") + "): Player " + playerNum + " completed task " + taskType.ToString());
            // server is inactive when you quit the game.
            // some tasks trigger when the stage ends or you quit the stage.
            // do this to not try to give out items for those tasks
            if (!NetworkServer.active)
                return;
            GiveReward(taskType, playerNum);

            // NetworkingAPI version
            // but this sends to all clients, doesn't it?
            //new TaskCompletionMessage((int)taskType, playerNum).Send(NetworkDestination.Clients);

            // old miniRPC version
            
            taskCompletionClient.Invoke((int)taskType, NetworkUser.readOnlyInstancesList[playerNum]);
            taskEndedClient.Invoke((int)taskType); 
        }

        void UpdateTaskProgress(TaskType taskType, float[] progress)
        {
            if (stageStartTasks is null || progress is null)
                return;
            int taskIndex = 0;
            for (int i = 0; i < stageStartTasks.Length; i++)
            {
                if(stageStartTasks[i] == (int)taskType)
                {
                    taskIndex = i;
                    break;
                }
            }
            // do a loop for tele tasks if I ever make those
            for (int i = 0; i < totalNumPlayers; i++)
            {
                float myProgress = progress[i];
                float rival = 0;
                for (int j = 0; j < totalNumPlayers; j++)
                {
                    if (j == i)
                        break;
                    if(progress[j] > rival)
                    {
                        rival = progress[j];
                    }
                }
                ProgressInfo p = new ProgressInfo(taskIndex, Mathf.Clamp01(myProgress), Mathf.Clamp01(rival));
                if (NetworkUser.readOnlyInstancesList is null || NetworkUser.readOnlyInstancesList.Count <= i)
                    return;
                updateProgressClient.Invoke(p, NetworkUser.readOnlyInstancesList[i]);
                //new UpdateProgressMessage(p, i).Send(NetworkDestination.Clients);
            }
        }

        void GiveReward(TaskType task, int playerNum)
        {
            // Some tasks end when the level ends.
            if (playerCharacterMasters is null)
                return;
            if(rewards[(int)task].type == RewardType.Item)
            {
                playerCharacterMasters[playerNum].inventory.GiveItem(rewards[(int)task].item.itemIndex, rewards[(int)task].numItems);
               
                // show text in chat
                PickupDef pickDef = PickupCatalog.GetPickupDef(rewards[(int)task].item);

                // i think it uses your current num of that item, it doesn't care how many you get at once so you don't have to pass it in
                typeof(GenericPickupController).InvokeMethod("SendPickupMessage", GetPlayerCharacterMaster(playerNum), rewards[(int)task].item);// (CharacterMaster master, PickupIndex pickupIndex)
                
            }
            else if(rewards[(int)task].type == RewardType.TempItem)
            {
                playerCharacterMasters[playerNum].inventory.GiveItem(rewards[(int)task].item.itemIndex, rewards[(int)task].numItems);

                PickupDef pickDef = PickupCatalog.GetPickupDef(rewards[(int)task].item);
                
                typeof(GenericPickupController).InvokeMethod("SendPickupMessage", GetPlayerCharacterMaster(playerNum), rewards[(int)task].item);

                // remove these items later
                // Record what items to remove
                RecordTempItems(playerNum, rewards[(int)task].item, rewards[(int)task].numItems);
            }
            else if(rewards[(int)task].type == RewardType.Command)
            {
                typeof(CommandArtifactManager).InvokeMethod("OnArtifactEnabled", RunArtifactManager.instance, RoR2Content.Artifacts.commandArtifactDef);
                if (typeof(CommandArtifactManager).GetField("commandCubePrefab") is null)
                {
                    //Chat.AddMessage("commandCubePrefab is null"); // always null??? Is GetField wrong? GetMember GetProperty GetFieldCached???
                }
                typeof(CommandArtifactManager).SetFieldValue<GameObject>("commandCubePrefab", Resources.Load<GameObject>("Prefabs/NetworkedObjects/CommandCube"));
                // need to make an item droplet. Specifically have it hit the ground
                PickupDropletController.onDropletHitGroundServer += TurnOffCommand;
                // technically, the next droplet to hit the ground is the command droplet. Whether it was created right here or if it was in the air already when this one was spawned.

                // create a droplet
                PickupIndex p = rewards[(int)task].item;
                PickupDropletController.CreatePickupDroplet(p, GetPlayerCharacterMaster(playerNum).GetBody().transform.position, GetPlayerCharacterMaster(playerNum).GetBody().transform.forward * 5);
            }
            else if(rewards[(int)task].type == RewardType.Drone)
            {
                RewardBuilder.GivePlayerDrone(playerNum, rewards[(int)task].dronePath);
            }
            else
            {
                // give gold or xp
            }
        }

        static void TurnOffCommand(ref GenericPickupController.CreatePickupInfo createPickupInfo, ref bool shouldSpawn)
        {
            typeof(CommandArtifactManager).InvokeMethod("OnArtifactDisabled", RunArtifactManager.instance, RoR2Content.Artifacts.commandArtifactDef);
        }

        void RecordTempItems(int playerNum, PickupIndex item, int count)
        {
            if(playerNum < 0)
            {
                Debug.Log("Didn't find a match. Couldn't record items");
                return;
            }
            TempItemLists[playerNum].Add(new TempItem(item, count));
        }

        void RemoveTempItems()
        {
            for (int i = 0; i < playerCharacterMasters.Count; i++)
            {
                List<TempItem> list = TempItemLists[i];
                int count = 0;
                int partials = 0; // temp items partially removed

                while(list.Count > 0)
                {
                    count++;
                    if (count > 50)
                    {
                        Debug.Log("Oops. Infinite loop. Quitting remove temp items");
                        return;
                    }

                    if (list.Count <= partials)
                        break;
                    TempItem temp = list[0+partials];

                    int toRemove = Math.Min(temp.count, 3); // remove 3/5 unless they have less than 3
                    temp.count -= toRemove; 
                    TempItemLists[i][0 + partials] = temp;

                    // maybe I should do a chat message like when someone picks up an item
                    // don't really want to spam chat. Multiple players might be losing multiple different items.
                    // could be local so you only see your own losses. That would be more work. This code runs on the server, not locally.
                    Debug.Log($"Removing {toRemove} {temp.item:g}");
                    playerCharacterMasters[i].inventory.RemoveItem(temp.item.itemIndex, toRemove);
                    if (temp.count <= 0)
                    {
                        list.RemoveAt(0);
                    }
                    else
                    {
                        partials++;
                    }
                }
            }
        }

        public static void StartPreonEvent()
        {
            instance.preonEventEqCache = new EquipmentIndex[playerCharacterMasters.Count];
            for (int i = 0; i < playerCharacterMasters.Count; i++)
            {
                instance.preonEventEqCache[i] = playerCharacterMasters[i].inventory.currentEquipmentIndex;
                playerCharacterMasters[i].inventory.SetEquipmentIndex(RoR2Content.Equipment.BFG.equipmentIndex);
                //playerCharacterMasters[i].inventory.GiveItem(ItemIndex.AutoCastEquipment); // annoying while testing stuff
                playerCharacterMasters[i].inventory.GiveItem(RoR2Content.Items.Talisman); // soulbound catalyst. Kills reduce eq cd
                playerCharacterMasters[i].inventory.GiveItem(RoR2Content.Items.EquipmentMagazine, 5);
            }

        }

        public static void EndPreonEvent()
        {
            for (int i = 0; i < playerCharacterMasters.Count; i++)
            {
                playerCharacterMasters[i].inventory.SetEquipmentIndex(instance.preonEventEqCache[i]);
                //playerCharacterMasters[i].inventory.RemoveItem(ItemIndex.AutoCastEquipment);
                playerCharacterMasters[i].inventory.RemoveItem(RoR2Content.Items.Talisman);
                playerCharacterMasters[i].inventory.RemoveItem(RoR2Content.Items.EquipmentMagazine, 5);
            }
        }

        public static void StartLockboxTask()
        {
            // give everyone a key
            // task is only available when someone gets the first key
            // but now you need a key to open the box
            // don't think I need to remove them after. 
            // It could get out of hand if you get multiple find the lockbox tasks
            for (int i = 0; i < playerCharacterMasters.Count; i++)
            {
                playerCharacterMasters[i].inventory.GiveItem(RoR2Content.Items.TreasureCache);
            }
        }   
    }
}
