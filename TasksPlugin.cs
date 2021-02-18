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
    [R2APISubmoduleDependency(nameof(UnlockablesAPI), nameof(ItemDropAPI), nameof(ItemDropAPI))]
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

        public static TasksPlugin instance;

        public IRpcAction<int> taskCompletionClient { get; set; }
        public IRpcAction<int> taskEndedClient { get; set; }
        //public IRpcAction<int[]> updateTasksClient { get; set; }
        public IRpcAction<TaskInfo> updateTaskClient { get; set; }

        Dictionary<uint, CharacterMaster> playerDict;
        // kinda bad form. There's already an array that holds the CharacterMasters. Why do I need to copy them?
        // I guess I can't guarentee it stays in the same order
        static List<CharacterMaster> playerCharacterMasters;
        int totalNumPlayers = 0;

        int totalNumTasks;
        Reward[] rewards;
        List<TempItem>[] TempItemLists;
        EquipmentIndex[] preonEventEqCache; // where your equipment is stored when the preon event starts and swaps it for a preon

        // Client
        TaskInfo[] currentTasks;
        GameObject[] tasksUIObjects;

        // Server
        int[] stageStartTasks;
        int[] teleStartTasks;

        bool activated = false;

        HUD hud;
        ObjectivePanelController panel;
        GameObject oTrackerPrefabCopy;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Awake is automatically called by Unity")]
        private void Awake() //Called when loaded by BepInEx.
        {
            Chat.AddMessage("Loaded Task plugin");

            if(instance is null)
            {
                instance = this;
            }

            On.RoR2.UserProfile.HasAchievement += (orig, self, param) =>
            {
                // This seems to work
                // It clears tasks when you load the game
                // Also when the task is activated
                // And when the game ends (when the game writes stats I think)
                // When you go to the logbook in the menu
                // It does get called a few times more than is really needed, but that's probably not a big performance hit
                // to revoke a task more often than is needed

                // Bc the tasks are temporary achievements, I never really want you to 'have' them. 
                // Achievements are a one-and-done thing. Having them implies they are over, but the tasks are reuseable
                // if HasAchievement() returns true, the game basically ignores it. So I just never let it return true for my tasks
                // It might have some problems in the future
                if(param.Contains("SOLRUN"))
                {
                    //Chat.AddMessage($"Matched Solrun. Removing Achievement: {param}");
                    self.RevokeAchievement(param);
                }
                return orig(self, param);
            };


            UnlockablesAPI.AddUnlockable<AirKills>(true);
            // AirKills.OnCompletion doesn't get called on the clients I don't believe.
            AirKills.OnCompletion += TaskCompletion;

            UnlockablesAPI.AddUnlockable<DamageMultipleTargets>(true);
            // OnCompletion is static for all tasks
            // and already accounts for knowing which task was completed
            // so I only need to sub to it once, not once for each different task
            //DamageMultipleTargets.OnCompletion += TaskCompletion;

            // How to make a new Task
            // Make the class
            // Add the UnlockablesAPI.AddUnlockable<>(true)
            // Update the TaskType enum (and the field in the new task class
            // Update the switch in GetTaskDescription (and the description field in the new task class)
            // change the achievement IDs

            UnlockablesAPI.AddUnlockable<DealDamageInTime>(true);
            UnlockablesAPI.AddUnlockable<StayInAir>(true);
            UnlockablesAPI.AddUnlockable<BiggestHit>(true);
            UnlockablesAPI.AddUnlockable<MostDistance>(true);
            UnlockablesAPI.AddUnlockable<PreonEvent>(true);


            Run.onRunStartGlobal += GameSetup;

            // Client only trigger
            // started, 100%, leave
            // server does those plus does interact, boss, interact
            // So the client tries to run RemoveTempItems and StageEnd

            TeleporterInteraction.onTeleporterBeginChargingGlobal += (TeleporterInteraction interaction) =>
            {
                // Once the tele event starts
                // So you interact, then wait a few secs, then this triggers, then the boss spawns
                Chat.AddMessage("TP event started");
                // I can add additional tasks here (or maybe in OnInteraction(tele) as that runs first
                // however, that runs twice. Once to start the tp, again to leave the stage

                
            };
            TeleporterInteraction.onTeleporterChargedGlobal += (TeleporterInteraction interaction) =>
            {
                // when the charge hits 100%
                Chat.AddMessage("TP charged to 100%");
                
            };
            TeleporterInteraction.onTeleporterFinishGlobal += (_) =>
            {
                // Runs when you click the tele to move to the next stage (after you kill the boss and charge the tele)
                Chat.AddMessage("TP finished and player chose to leave");
                if (NetworkServer.active)
                {
                    StageEnd();
                }
            };
            GlobalEventManager.OnInteractionsGlobal += (Interactor interactor, IInteractable interactable, GameObject go) =>
            {
                // interactor is the player
                //interactor.GetComponent<CharacterBody>();
                
                // this works
                // it gets called when you start the tp event and again when you interact with the tp to leave
                if(go?.GetComponent<TeleporterInteraction>())
                {
                    // this might be true for interacting with the end tp to switch to loop mode
                    Chat.AddMessage("Interacted with TP");
                }
            };
            BossGroup.onBossGroupDefeatedServer += (BossGroup group) =>
            {
                // this works. Timer too
                Chat.AddMessage($"Boss defeated in {group.fixedTimeSinceEnabled} seconds");
            };

            

            var miniRpc = MiniRpc.CreateInstance(GUID);
            taskCompletionClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, int task) =>
            {
                // code that runs on the client
                // user specifies which user I believe so I don't have to check
                Chat.AddMessage($"Trying to make the popup on the client. User: {user} Task: {task}");
                OnPopup?.Invoke(task);
                RemoveObjectivePanel(task);
            });

            taskEndedClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, int task) =>
            {
                Chat.AddMessage($"Task {task} ended. Removing UI");
                // task ended
                RemoveObjectivePanel(task);
            });

            updateTaskClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, TaskInfo taskInfo) =>
            {
                Chat.AddMessage($"UpdateTaskClient: {taskInfo}");
                if(currentTasks is null || currentTasks.Length != taskInfo.total)
                {
                    currentTasks = new TaskInfo[taskInfo.total];
                }

                currentTasks[taskInfo.index] = taskInfo;

                UpdateTasksUI(taskInfo.index);
            });


            On.RoR2.UI.HUD.Awake += (self, orig) =>
            {
                // This works now
                // Gotta grab panel from orig instead of trying to find it
                // and have to remember to activate the spawned GO
                // This also gets called at the start of each stage. Still need to test more to see if player bodies are nul or not
                // GenerateTasks waits 3 seconds and that seems to do it
                self(orig);
                hud = orig;
                panel = orig.objectivePanelController;

                int numberOfStageTasks = 6; 
                tasksUIObjects = new GameObject[numberOfStageTasks];

                if(NetworkServer.active)
                    GenerateTasks(numberOfStageTasks);
                
            };   
        }

        void UpdateTasksUI(int taskIndex, string text = "")
        {
            if (tasksUIObjects[taskIndex] is null)
            {
                tasksUIObjects[taskIndex] = Instantiate(panel.objectiveTrackerPrefab, hud.objectivePanelController.transform);
            }
            tasksUIObjects[taskIndex].SetActive(true);

            TMPro.TextMeshProUGUI textMeshLabel = tasksUIObjects[taskIndex].transform.Find("Label").GetComponent<TMPro.TextMeshProUGUI>();
            if (textMeshLabel != null)
            {
                textMeshLabel.text = (text == "") ? currentTasks[taskIndex].description : text;
            }
        }

        void RemoveObjectivePanel(int taskType)
        {
            for (int i = 0; i < tasksUIObjects.Length; i++)
            {
                if(currentTasks[i].taskType == taskType)
                {
                    if(tasksUIObjects[i] is null)
                    {
                        Chat.AddMessage($"UI Object {i} was null");
                        break;
                    }

                    // TODO: Do something to show it was completed other than just hiding it
                    tasksUIObjects[i].SetActive(false);
                    // should I just destroy them? Doesn't seem to help
                    // They might get removed twice. Once if you complete them and then again at the end
                    // destroying them doesn't make them null so it's hard to check for
                    //Destroy(tasksUIObjects[i]);
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

            totalNumTasks = Enum.GetNames(typeof(TaskType)).Length;
            rewards = new Reward[totalNumTasks];

            // -1 forces it to match for deactivation purposes
            // if(id == myId || id < 0)
            // Means I don't have to iterate over every task id and check if it matches n*n times
            OnDeactivate?.Invoke(-1);


            // Moved to UI.Awake bc it's called every stage
            //GenerateTasks(4);

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
            //if(Input.GetKeyDown(KeyCode.F2))
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
            yield return new WaitForSeconds(3); // 3 sec is arbitrary, but seems to work. Could fail if stages later in the run take longer to load maybe
            StartTasks(numTasks);
        }

        void StartTasks(int numTasks, bool teleTasks = false)
        {
            int[] taskIDNumbers = GetRandomUniqueTasks(numTasks);

            if(teleTasks)
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
                rewards[taskIDNumbers[i]] = CreateRandomReward();

                // These 2 lines for debugging
                int r = taskIDNumbers[i];
                Chat.AddMessage(String.Format("Task: {0}. Reward: {1} From r: {2}", ((TaskType)r).ToString(), rewards[r].ToString(), r));

                OnActivate?.Invoke(taskIDNumbers[i], totalNumPlayers);
            }

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
                    //Chat.AddMessage($"Count is {NetworkUser.readOnlyInstancesList.Count}");
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

        void StageEnd()
        {
            // Do this before ending all tasks
            // some tasks are only finished when the stage ends (deal the most damage, etc)
            // So they need to give their reward after temp items are removed in case they give temp items
            RemoveTempItems();
            EndAllTasks();
        }

        int[] GetRandomUniqueTasks(int count)
        {
            // all possible tasks. Ignore the base task
            List<int> allTasks = new List<int>(totalNumTasks - 1);
            int[] results = new int[count];

            for (int i = 0; i < totalNumTasks-1; i++)
            {
                // +1 to ignore the base task
                allTasks.Add(i + 1);
            }

            if (count > allTasks.Count)
            {
                // uh oh
                Chat.AddMessage($"Not enough tasks({allTasks.Count}). Wanted {count}.");
            }
            

            for (int i = 0; i < results.Length; i++)
            {
                int r = UnityEngine.Random.Range(0, allTasks.Count);
                results[i] = allTasks[r];
                allTasks.RemoveAt(r);
            }

            return results;
        }

        string GetTaskDescription(int taskType)
        {
            switch((TaskType)taskType)
            {
                case TaskType.AirKills:
                    return AirKills.description;

                case TaskType.DamageMultiple:
                    return DamageMultipleTargets.description;

                case TaskType.DamageInTime:
                    return DealDamageInTime.description;

                case TaskType.StayInAir:
                    return StayInAir.description;

                case TaskType.BiggestHit:
                    return BiggestHit.description;

                case TaskType.MostDistance:
                    return MostDistance.description;

                case TaskType.PreonEvent:
                    return PreonEvent.description;
            }

            return "";
        }

        void NotificationTest()
        {
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
            StartCoroutine(MoveNotificationLater(n, new Vector3(1920f / 2, 1080f / 2, 0)));

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
            Chat.AddMessage("SERVER("+(NetworkServer.active?"active":"not active")+"): Player "+playerNum + " completed task " + taskType.ToString());
            // this works at least
            activated = false;
            //GiveRandomItem(netID);
            GiveReward(taskType, playerNum);
            //RpcTaskCompletion(taskType, playerNum);
            // This should send the message to the client
            // Why is it backwards from how I wrote it? Weird
            // Does this run on each client or jsut the specific one?
            taskCompletionClient.Invoke((int)taskType, NetworkUser.readOnlyInstancesList[playerNum]);
            taskEndedClient.Invoke((int)taskType); // hope this sends to everyone
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

        void GiveRandomItem(int playerNum)
        {
            playerCharacterMasters[playerNum].inventory.GiveRandomItems(1);
        }

        void GiveReward(TaskType task, int playerNum)
        {
            Chat.AddMessage($"Giving a reward. Notif Queue size: {NotificationQueue.readOnlyInstancesList.Count}");
            if(rewards[(int)task].type == RewardType.Item)
            {
                playerCharacterMasters[playerNum].inventory.GiveItem(rewards[(int)task].item.itemIndex, rewards[(int)task].numItems);
                // I'm not sure what readOnlyInstanceList[0] is
                // Are there more than one? GenericPickupController loops over all of them and runs OnPickup on all of them
                NotificationQueue.readOnlyInstancesList[0].OnPickup(GetPlayerCharacterMaster(playerNum), rewards[(int)task].item);
               
                // show text in chat
                PickupDef pickDef = PickupCatalog.GetPickupDef(rewards[(int)task].item);
                Chat.AddPickupMessage(GetPlayerCharacterMaster(playerNum).GetBody(), pickDef.nameToken, pickDef.baseColor, 1);

                // This might not be any different...
                /*
                Chat.AddMessage(new Chat.PlayerPickupChatMessage
                {
                    subjectAsCharacterBody = GetPlayerCharacterMaster(playerNum).GetBody(),
                    baseToken = "PLAYER_PICKUP",
                    pickupToken = pickDef.nameToken,
                    pickupColor = pickDef.baseColor,
                    pickupQuantity = 1
                }.ConstructChatString() + " This might play on each client like it's supposed to");
                */
            }
            else if(rewards[(int)task].type == RewardType.TempItem)
            {
                playerCharacterMasters[playerNum].inventory.GiveItem(rewards[(int)task].item.itemIndex, rewards[(int)task].numItems);
                NotificationQueue.readOnlyInstancesList[0].OnPickup(GetPlayerCharacterMaster(playerNum), rewards[(int)task].item);

                PickupDef pickDef = PickupCatalog.GetPickupDef(rewards[(int)task].item);
                Chat.AddPickupMessage(GetPlayerCharacterMaster(playerNum).GetBody(), pickDef.nameToken, pickDef.baseColor, Convert.ToUInt32(rewards[(int)task].numItems));
                /*
                Chat.AddMessage(new Chat.PlayerPickupChatMessage
                {
                    subjectAsCharacterBody = GetPlayerCharacterMaster(playerNum).GetBody(),
                    baseToken = "PLAYER_PICKUP",
                    pickupToken = pickDef.nameToken,
                    pickupColor = pickDef.baseColor,
                    pickupQuantity = Convert.ToUInt32(rewards[(int)task].numItems)
                }.ConstructChatString() + " This might play on each client like it's supposed to");
                */
                // remove these items later
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

        void RecordTempItems(int playerNum, PickupIndex item, int count)
        {
            
            if(playerNum < 0)
            {
                Chat.AddMessage("Didn't find a match. Couldn't record items");
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
                    
                    playerCharacterMasters[i].inventory.RemoveItem(temp.item.itemIndex, temp.count);
                    list.RemoveAt(0);
                }
            }
        }

        Reward CreateRandomReward()
        {

            // ==== Type of Reward
            WeightedSelection<RewardType> typeSelection = new WeightedSelection<RewardType>();

            typeSelection.AddChoice(RewardType.Item, 2);
            typeSelection.AddChoice(RewardType.TempItem, 1);

            RewardType type = typeSelection.Evaluate(UnityEngine.Random.value);


            // ===== Item
            WeightedSelection<ItemDropLocation> chestSelection = new WeightedSelection<ItemDropLocation>();

            // wiki says weights are small:24, med:4, large:2, specialty:2
            // specialty are so low just to be more rare, not bc they are too powerful
            // so I'm fine with increasing their weight
            chestSelection.AddChoice(ItemDropLocation.SmallChest, 24);
            chestSelection.AddChoice(ItemDropLocation.MediumChest, 4);
            chestSelection.AddChoice(ItemDropLocation.UtilityChest, 8);
            chestSelection.AddChoice(ItemDropLocation.DamageChest, 8);
            chestSelection.AddChoice(ItemDropLocation.HealingChest, 8);
            chestSelection.AddChoice(ItemDropLocation.LargeChest, 1); // legendary
            

            // Can I give players use items in the same way as I give them regular items?
            //chestSelection.AddChoice(ItemDropLocation.EquipmentChest, 20);

            // EntityStates.ScavMonster.FindItem uses random.value in evaluate. It returns a value between 0.0 and 1.0
            // multiplies this value by the total weight then figures out which selection that corresponds to
            // Using the example values of 24, 4, 8, 8, 8, 8, 1 = 61
            // 61 * value = 21(for example) would map to a small chest because 21 < 24
            // 43 would map to 24 +4 +8 =36 < 43 < 24+4+8+8 =44 so the damage chest
            ItemDropLocation chest = chestSelection.Evaluate(UnityEngine.Random.value);

            // get a random item from a specific chest
            // and turn it into an itemIndex (as opposed to a pickupIndex which seems to be depreciated
            PickupIndex pickupIndex = ItemDropAPI.GetSelection(chest, UnityEngine.Random.value);
            //PickupDef def = PickupCatalog.GetPickupDef(pickupIndex);
            //ItemIndex item = def.itemIndex;
            // ===== End Item


            Chat.AddMessage($"Reward created: {pickupIndex:g} from a {chest:g}");

            Reward reward = new Reward(type, pickupIndex, (type == RewardType.TempItem) ? 5 : 1, false, 100, 100);
            return reward;
        }

        public static void StartPreonEvent()
        {
            instance.preonEventEqCache = new EquipmentIndex[playerCharacterMasters.Count];
            for (int i = 0; i < playerCharacterMasters.Count; i++)
            {
                instance.preonEventEqCache[i] = playerCharacterMasters[i].inventory.currentEquipmentIndex;
                playerCharacterMasters[i].inventory.SetEquipmentIndex(EquipmentIndex.BFG);
                //playerCharacterMasters[i].inventory.GiveItem(ItemIndex.AutoCastEquipment); // annoying while testing stuff
                playerCharacterMasters[i].inventory.GiveItem(ItemIndex.EquipmentMagazine, 5);
            }

        }

        public static void EndPreonEvent()
        {
            for (int i = 0; i < playerCharacterMasters.Count; i++)
            {
                playerCharacterMasters[i].inventory.SetEquipmentIndex(instance.preonEventEqCache[i]);
                //playerCharacterMasters[i].inventory.RemoveItem(ItemIndex.AutoCastEquipment);
                playerCharacterMasters[i].inventory.RemoveItem(ItemIndex.EquipmentMagazine, 5);
            }
        }

        public struct Reward
        {
            public Reward(RewardType _type, PickupIndex _item, int _numItems, bool _temporary, int _gold, int _xp)
            {
                type = _type;
                item = _item;
                numItems = _numItems;
                temporary = _temporary;
                gold = _gold;
                xp = _xp;
            }

            public RewardType type;
            public PickupIndex item;
            public int numItems;
            public bool temporary;
            public int gold;
            public int xp;

            public override string ToString() => $"{type:g}, {item:g}";
        }

        public struct TempItem
        {
            public TempItem(PickupIndex _item, int _count)
            {
                item = _item;
                count = _count;
            }
            public PickupIndex item;
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
    public enum TaskType { Base, AirKills, DamageMultiple, DamageInTime, StayInAir, BiggestHit, MostDistance, PreonEvent };

}
