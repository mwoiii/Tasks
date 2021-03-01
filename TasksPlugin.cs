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
using UnityEngine.UI;

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

        public static TasksPlugin instance;

        public static event Action<int, int> OnActivate;
        public static event Action<int> OnDeactivate;
        public static event Action OnResetAll;
        public static event Action<int> OnPopup;
        public static event Action<int, SkillSlot> OnAbilityUsed;

        public IRpcAction<int> taskCompletionClient { get; set; }
        public IRpcAction<int> taskEndedClient { get; set; }
        public IRpcAction<TaskInfo> updateTaskClient { get; set; }

        
        static List<CharacterMaster> playerCharacterMasters;
        int totalNumPlayers = 0;

        int totalNumTasks;
        Reward[] rewards;
        List<TempItem>[] TempItemLists;
        EquipmentIndex[] preonEventEqCache; // where your equipment is stored when the preon event starts and swaps it for a preon

        // Server
        int[] stageStartTasks;
        int[] teleStartTasks;

        // Client
        TaskInfo[] currentTasks;
        GameObject[] tasksUIObjects;

        HUD hud;
        ObjectivePanelController panel;
        GameObject itemIconPrefabCopy;

        // Testing
        bool notifTestingFormat = true;


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Awake is automatically called by Unity")]
        private void Awake() //Called when loaded by BepInEx.
        {
            Chat.AddMessage("Loaded Task plugin");

            if(instance is null)
            {
                instance = this;
            }

            // How to make a new Task
            // Make the class
            // Add the UnlockablesAPI.AddUnlockable<>(true)
            // Update the TaskType enum in Task.cs (and the field in the new task class)
            // Update the switch in GetTaskDescription (and the description field in the new task class)
            // change the achievement IDs
            Task.OnCompletion += TaskCompletion;
            
            UnlockablesAPI.AddUnlockable<AirKills>(true);
            UnlockablesAPI.AddUnlockable<DamageMultipleTargets>(true);
            UnlockablesAPI.AddUnlockable<DealDamageInTime>(true);
            UnlockablesAPI.AddUnlockable<StayInAir>(true);
            UnlockablesAPI.AddUnlockable<BiggestHit>(true);
            UnlockablesAPI.AddUnlockable<MostDistance>(true);
            UnlockablesAPI.AddUnlockable<PreonEvent>(true);
            UnlockablesAPI.AddUnlockable<FarthestAway>(true);
            UnlockablesAPI.AddUnlockable<FailShrine>(true);
            UnlockablesAPI.AddUnlockable<OpenChests>(true);
            UnlockablesAPI.AddUnlockable<StartTeleporter>(true);
            UnlockablesAPI.AddUnlockable<UsePrinters>(true);
            UnlockablesAPI.AddUnlockable<OrderedSkills>(true);
            UnlockablesAPI.AddUnlockable<DontUseSkill>(true);


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
                PickupIndex p = new PickupIndex(ItemIndex.Bear);
                PickupDropletController.CreatePickupDroplet(p, GetPlayerCharacterMaster(0).GetBody().transform.position, GetPlayerCharacterMaster(0).GetBody().transform.forward);
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                for (int i = 1; i < totalNumTasks; i++)
                {
                    CreateNotification(i);
                }
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                notifTestingFormat = !notifTestingFormat;
            }

        }

        void GameSetup(Run run)
        {
            // run.livingPlayerCount
            // run.participatingPlayerCount is this the total players?
            if (!NetworkServer.active)
            {
                // this is the client
                return;
            }

            Chat.AddMessage($"Number of players: {run.participatingPlayerCount} Living Players: {run.livingPlayerCount}");
            totalNumPlayers = run.participatingPlayerCount;
            playerCharacterMasters = new List<CharacterMaster>(totalNumPlayers);

            PopulatePlayerCharaterMasterList();
            PopulateTempItemLists();

            totalNumTasks = Enum.GetNames(typeof(TaskType)).Length;
            rewards = new Reward[totalNumTasks];

            Chat.AddMessage("ItemInventoryDisplay");
            // why did this used to work?
            ItemInventoryDisplay display = FindObjectOfType<ItemInventoryDisplay>();
            if (display is null)
            {
                // why are you null
                Chat.AddMessage("Display is null");
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

        void PopulatePlayerCharaterMasterList()
        {
            for (int i = 0; i < CharacterMaster.readOnlyInstancesList.Count; i++)
            {
                Chat.AddMessage($"Name: {CharacterMaster.readOnlyInstancesList[i].name} NetID: {CharacterMaster.readOnlyInstancesList[i].netId} LocalPlayer: {CharacterMaster.readOnlyInstancesList[i].isLocalPlayer}");
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
            var miniRpc = MiniRpc.CreateInstance(GUID);
            taskCompletionClient = miniRpc.RegisterAction(Target.Client, (NetworkUser user, int task) =>
            {
                // code that runs on the client
                // user specifies which user I believe so I don't have to check
                Chat.AddMessage($"Trying to make the popup on the client. User: {user} Task: {task}");
                CreateNotification(task);
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
                if (currentTasks is null || currentTasks.Length != taskInfo.total)
                {
                    currentTasks = new TaskInfo[taskInfo.total];
                }

                currentTasks[taskInfo.index] = taskInfo;
                rewards[(int)taskInfo.taskType] = taskInfo.reward;

                UpdateTasksUI(taskInfo.index);
            });
        }

        void SetGameHooks()
        {
            // Removes my task achievements so they can be achieved again
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
                if (param.Contains("SOLRUN"))
                {
                    //Chat.AddMessage($"Matched Solrun. Removing Achievement: {param}");
                    self.RevokeAchievement(param);
                }
                return orig(self, param);
            };


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
                        //Chat.AddMessage($"Server Active?{NetworkServer.active} Player {i} used {skill:g}");
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
                if (go?.GetComponent<ShopTerminalBehavior>())
                {
                    // Multishops AND 3D printers
                    // [Info   : Unity Log] Interacted with multishop. InterType: RoR2.PurchaseInteraction
                    Chat.AddMessage("Interacted with multishop. InterType: " + interactableType + " name: " + go.name);
                    // MultiShopTerminal(Clone)
                    // DuplicatorLarge(Clone) --- Duplicator(Clone)
                }
                if (go?.GetComponent<ChestBehavior>())
                {
                    // damage chest, eq chest, small chest all worked
                    // [Info   : Unity Log] Interacted with chest. InterType: RoR2.PurchaseInteraction
                    Chat.AddMessage("Interacted with chest. InterType: " + interactableType + " name: " + go.name);
                    // CategoryChestUtility(Clone) --- CategoryChestDamage(Clone)
                    // Chest1 --- Chest2
                    // EquipmentBarrel(Clone)
                }
                if (go?.GetComponent<BarrelInteraction>())
                {
                    // [Info   : Unity Log] Interacted with a barrel. InterType: RoR2.BarrelInteraction
                    Chat.AddMessage("Interacted with a barrel. InterType: " + interactableType + " name: " + go.name);
                    // Barrel1(Clone)
                }
                if (go?.GetComponent<PrintController>())
                {
                    Chat.AddMessage("Interacted with a 3D printer. InterType: " + interactableType + " name: " + go.name);
                }
                // when you use a 3D printer or eq drone i think or pool prob
                // PurchaseInteraction.onItemSpentOnPurchase += method
                // purchaseInteraction.gameObject.name.Contains("Duplicator")

                /*
                // maybe this works, but can't put it inside this method. This is how the vanilla achieve works
                EntityStates.TimedChest.Opening.onOpened += () =>
                {
                    Chat.AddMessage("Opened a timed chest");
                };
                */
                // this works
                // it gets called when you start the tp event and again when you interact with the tp to leave
                if (go?.GetComponent<TeleporterInteraction>())
                {
                    // this might be true for interacting with the end tp to switch to loop mode
                    Chat.AddMessage("Interacted with TP");
                }
            };
            TeleporterInteraction.onTeleporterBeginChargingGlobal += (TeleporterInteraction interaction) =>
            {
                // Once the tele event starts
                // triggers on the client
                // So you interact, then wait a few secs, then this triggers, then the boss spawns
                Chat.AddMessage("TP event started");
                // I can add additional tasks here (or maybe in OnInteraction(tele) as that runs first
                // however, that runs twice. Once to start the tp, again to leave the stage
            };
            TeleporterInteraction.onTeleporterChargedGlobal += (TeleporterInteraction interaction) =>
            {
                // when the charge hits 100%
                // triggers on the client
                Chat.AddMessage("TP charged to 100%");

            };
            TeleporterInteraction.onTeleporterFinishGlobal += (_) =>
            {
                // Runs when you click the tele to move to the next stage (after you kill the boss and charge the tele)
                // triggers on the client
                Chat.AddMessage("TP finished and player chose to leave");
                if (NetworkServer.active)
                {
                    StageEnd();
                }
            };
            BossGroup.onBossGroupDefeatedServer += (BossGroup group) =>
            {
                // this works. Timer too
                Chat.AddMessage($"Boss defeated in {group.fixedTimeSinceEnabled} seconds");
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

                int numberOfStageTasks = 5;
                tasksUIObjects = new GameObject[numberOfStageTasks];

                if (NetworkServer.active)
                    GenerateTasks(numberOfStageTasks);
            };
        }

        void UpdateTasksUI(int taskIndex, string text = "")
        {
            if (tasksUIObjects[taskIndex] is null)
            {
                tasksUIObjects[taskIndex] = Instantiate(panel.objectiveTrackerPrefab, hud.objectivePanelController.transform);

                // rewards is totalNumTasks long
                // taskIndex is like 9 at most.
                int rewardIndex = currentTasks[taskIndex].taskType;

                tasksUIObjects[taskIndex].SetActive(true);
                if(itemIconPrefabCopy is null)
                {
                    itemIconPrefabCopy = FindObjectOfType<ItemInventoryDisplay>().itemIconPrefab;
                }
                ItemIcon icon = Instantiate(itemIconPrefabCopy, tasksUIObjects[taskIndex].transform).GetComponent<ItemIcon>();
                icon.SetItemIndex(rewards[rewardIndex].item.itemIndex, rewards[rewardIndex].numItems);
                
                RectTransform rect = icon.rectTransform;
                rect.localScale = Vector3.one * 0.5f;
                
                Image checkBox = tasksUIObjects[taskIndex].transform.Find("Checkbox").GetComponent<Image>();
                checkBox.color = new Color(0, 0, 0, 0); // invisible
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
                    //tasksUIObjects[i].SetActive(false);
                    TMPro.TextMeshProUGUI textMeshLabel = tasksUIObjects[i].transform.Find("Label").GetComponent<TMPro.TextMeshProUGUI>();
                    if(textMeshLabel is null)
                    {
                        Chat.AddMessage("Couldn't strikethrough");
                    }
                    else
                    {
                        textMeshLabel.color = Color.grey;
                        textMeshLabel.fontStyle |= TMPro.FontStyles.Strikethrough;

                        ItemIcon icon = tasksUIObjects[i].GetComponentInChildren<ItemIcon>();
                        if(icon)
                            icon.image.color = Color.grey;
                    }
                    
                    // should I just destroy them? Doesn't seem to help
                    // They might get removed twice. Once if you complete them and then again at the end
                    // destroying them doesn't make them null so it's hard to check for
                    //Destroy(tasksUIObjects[i]);
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
                rewards[taskIDNumbers[i]] = RewardBuilder.CreateRandomReward();

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
                        TaskInfo info = new TaskInfo(taskIDNumbers[j], GetTaskDescription(taskIDNumbers[j]), false, j, taskIDNumbers.Length, rewards[taskIDNumbers[j]]);
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

                case TaskType.FarthestAway:
                    return FarthestAway.description;

                case TaskType.FailShrine:
                    return FailShrine.description;

                case TaskType.OpenChests:
                    return OpenChests.description;

                case TaskType.StartTele:
                    return StartTeleporter.description;

                case TaskType.UsePrinters:
                    return UsePrinters.description;

                case TaskType.OrderedSkills:
                    return OrderedSkills.description;

                case TaskType.BadSkill:
                    return DontUseSkill.description;
            }

            return "";
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

                    if (notifTestingFormat)
                    {
                        // 425, 326 + 1.5fx is alright. Long titles might bleed over into the objective panel
                        n.GenericNotification.transform.localPosition = new Vector3(435, 326, 0) + 1.5f * tasksUIObjects[i].transform.localPosition;
                        n.RootObject.transform.Find("TextArea").transform.localScale = Vector3.one * 1.5f; // embiggen text
                    }
                    else
                    {
                        // now that i'm jsut doing the strikethrough
                        n.GenericNotification.transform.SetParent(tasksUIObjects[i].transform, false);
                        n.GenericNotification.transform.localPosition = new Vector3(-200, -32, 0); // close, but the scale has been reset.
                        // This version works, but it's just a little off
                        // I'd need to reduce the scale, and increase the text scale
                        // and move it closer. -200, -32 is alright, but it could be closer. And the other version already has that stuff done
                    }
                  
                    break;
                }
            }
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
            n.GenericNotification.transform.position = new Vector3(1920f / 2, 1080f / 2, 0);
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
                    n.GetDescription = () => $"Description: {tempi * 200}, {tempj * 200}";
                    n.GetTitle = () => $"Title: {tempi}, {tempj}";
                    //n.SetPosition(new Vector3(i/3f, j/3f, 0));
                    //n.Parent = NotificationQueue.readOnlyInstancesList[0].transform;
                    //n.Parent = go.transform;
                    n.enabled = true;

                    // Seems like these correspond to 1920x1080
                    n.GenericNotification.transform.position = new Vector3(tempi * 200, tempj * 200, 0);
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
            Chat.AddMessage("CharMaster didn't match any players");
            return -1;
        }

        public static CharacterMaster GetPlayerCharacterMaster(int playerNum)
        {
            return playerCharacterMasters[playerNum];
        }

        void TaskCompletion(TaskType taskType, int playerNum)
        {
            Chat.AddMessage("SERVER(" + (NetworkServer.active ? "active" : "not active") + "): Player " + playerNum + " completed task " + taskType.ToString());
            // this works at least
            GiveReward(taskType, playerNum);

            taskCompletionClient.Invoke((int)taskType, NetworkUser.readOnlyInstancesList[playerNum]);
            taskEndedClient.Invoke((int)taskType); // hope this sends to everyone
        }

        void GiveReward(TaskType task, int playerNum)
        {
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
                //Chat.AddMessage($"List count: {list.Count}");
                while(list.Count > 0)
                {
                    count++;
                    if (count > 50)
                    {
                        Chat.AddMessage("Oops. Infinite loop. Quitting remove temp items");
                        return;
                    }
                        
                    TempItem temp = list[0];
                    //Chat.AddMessage($"Removing {temp.count} {temp.item:g}");
                    // maybe I should do a chat message like when someone picks up an item

                    playerCharacterMasters[i].inventory.RemoveItem(temp.item.itemIndex, temp.count);
                    list.RemoveAt(0);
                }
            }
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

        public class TaskInfo : MessageBase
        {
            public int taskType;
            public string description;
            public bool completed;
            public int index;
            public int total;
            public Reward reward;

            public override void Serialize(NetworkWriter writer)
            {
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
            }

            public override void Deserialize(NetworkReader reader)
            {
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

                PickupIndex p = new PickupIndex(item);
                reward = new Reward((RewardType)type, p, numItems, temp, gold, xp);
            }
            public TaskInfo(int _type, string _description, bool _completed, int _index, int _total, Reward _reward)
            {
                taskType = _type;
                description = _description;
                completed = _completed;
                index = _index;
                total = _total;
                reward = _reward;
            }

            public override string ToString() => $"TaskInfo: {taskType}, {description}, {completed}, {index}/{total}";
        }
    }
    

}
