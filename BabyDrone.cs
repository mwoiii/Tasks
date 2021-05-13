using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RoR2;
using UnityEngine;

namespace Tasks
{
    class BabyDrone : Task
    {
        public override TaskType type { get; } = TaskType.BabyDrone;
        protected override string name { get; } = "Baby Drone"; // is this ever used?

        CharacterMaster[] drones;
        CharacterMaster createdDrone;

        bool[] playerFailed;
        int numPlayersFailed;
        bool active;

        public override bool CanActivate(int numPlayers)
        {
            return numPlayers > 1;
        }

        public override string GetDescription()
        {
            return "Keep your drone alive";
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set Hooks in BabyDrone. {numPlayers} players");

            base.SetHooks(numPlayers);
            drones = new CharacterMaster[numPlayers];
            playerFailed = new bool[numPlayers];
            numPlayersFailed = 0;
            active = true;

            for (int i = 0; i < numPlayers; i++)
            {
                playerFailed[i] = false;
                SpawnDrone(i);

            }

            GlobalEventManager.onCharacterDeathGlobal += OnKill;
        }

        protected override void Unhook()
        {
            if (!active)
                return;
            active = false;

            // end of stage. Are there multiple winners?
            if(totalNumberPlayers-numPlayersFailed > 1)
            {
                for (int i = 0; i < playerFailed.Length; i++)
                {
                    if(!playerFailed[i])
                    {
                        UpdateProgressMultiWinner();
                        CompleteTask(i);
                    }
                }
            }

            GlobalEventManager.onCharacterDeathGlobal -= OnKill;


            ResetProgress();
            base.Unhook();
        }

        void UpdateProgress()
        {
            for (int i = 0; i < progress.Length; i++)
            {
                // if you've failed, your bar is 0
                // if you haven't, bar shows how many people have failed.
                // when it fills up, that means you won
                progress[i] = playerFailed[i] ? 0 : (numPlayersFailed / (totalNumberPlayers - 1));
            }
            base.UpdateProgress(progress);
        }

        void UpdateProgressMultiWinner()
        {
            for (int i = 0; i < progress.Length; i++)
            {
                progress[i] = playerFailed[i] ? 0 : 1;
            }
            base.UpdateProgress(progress);
        }

        public void OnKill(DamageReport damageReport)
        {
            if (damageReport is null) return;
            if (damageReport.victimMaster is null) return;

            for (int i = 0; i < drones.Length; i++)
            {
                if (!playerFailed[i])
                {
                    if (damageReport.victimMaster == drones[i])
                    {
                        //Chat.AddMessage(GetDroneDeathMessage(drones[i].name));
                        R2API.Utils.ChatMessage.Send(GetDroneDeathMessage(drones[i].name)); // should go to each player?
                        
                        // a drone died
                        playerFailed[i] = true;
                        numPlayersFailed++;
                        UpdateProgress();
                        //Chat.AddMessage($"Player {i} failed Baby Drone");
                        if(numPlayersFailed >= totalNumberPlayers-1)
                        {
                            for (int j = 0; j < playerFailed.Length; j++)
                            {
                                if(!playerFailed[j])
                                {
                                    CompleteTask(j);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }


        void SpawnDrone(int playerNum)
        {
            CharacterBody playerBody = TasksPlugin.GetPlayerCharacterMaster(playerNum).GetBody();

            GameObject groundDrone = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscbrokendrone1"), new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.Direct,
                position = playerBody.transform.position + new Vector3(1, 0, 0)
            }, new Xoroshiro128Plus(0)));

            PurchaseInteraction purchase = groundDrone.GetComponent<PurchaseInteraction>();
            purchase.cost = 0;
            Interactor i = playerBody.GetComponent<Interactor>();
            if (i)
            {
                // this intercepts the real drone when it is spawned
                On.RoR2.MasterSummon.Perform += GrabDrone;
                // turn the drone on the ground (that you can purchase)
                // into a drone that flies around
                purchase.OnInteractionBegin(i);

                On.RoR2.MasterSummon.Perform -= GrabDrone;


                string token = $"BABY_DRONE_{playerNum}";
                Language.currentLanguage.SetStringByToken(token, GetDroneName(playerNum));

                CharacterBody droneBody = createdDrone.GetBody();
                droneBody.baseNameToken = token;

                droneBody.baseRegen = 0;

                drones[playerNum] = createdDrone;
                TasksPlugin.instance.StartCoroutine(DamageDrone(droneBody));
            }
        }

        CharacterMaster GrabDrone(On.RoR2.MasterSummon.orig_Perform orig_Perform, MasterSummon self)
        {
            CharacterMaster m = orig_Perform(self);
            createdDrone = m;
            return m;
        }

        string GetDroneName(int playerNum)
        {
            string displayName = TasksPlugin.GetPlayerCharacterMaster(playerNum).GetComponent<PlayerCharacterMasterController>().GetDisplayName();
            WeightedSelection<string> droneNames = new WeightedSelection<string>(14);
            droneNames.AddChoice($"{displayName} Junior", 1);
            droneNames.AddChoice($"{displayName} Jr.", 1);
            droneNames.AddChoice($"Baby {displayName}", 1);
            droneNames.AddChoice($"Little {displayName}", 1);
            droneNames.AddChoice($"Li'l {displayName}", 1);
            droneNames.AddChoice($"{displayName} the Second", 1);
            droneNames.AddChoice($"{displayName} the Third", 1);
            droneNames.AddChoice($"{displayName} II", 1);
            droneNames.AddChoice($"{displayName} III", 1);
            droneNames.AddChoice($"{displayName} the Younger", 1);
            droneNames.AddChoice($"Mini {displayName}", 1);
            droneNames.AddChoice($"{displayName} Mk II", 1);
            droneNames.AddChoice($"{displayName} 2.0", 1);
            droneNames.AddChoice($"{displayName} (Clone)", 1);


            return droneNames.Evaluate(UnityEngine.Random.value);
        }

        string GetDroneDeathMessage(string droneName)
        {

            WeightedSelection<string> messages = new WeightedSelection<string>();
            messages.AddChoice($"{droneName} died. And it was all your fault.", 1);
            messages.AddChoice($"{droneName} died. And you just let it happen.", 1);
            messages.AddChoice($"{droneName} died because you didn't love him enough.", 1);
            messages.AddChoice($"{droneName} died. Play on an easier difficulty so this doesn't happen again.", 1);
            messages.AddChoice($"You killed {droneName}. You monster.", 1);
            messages.AddChoice($"You let {droneName} die. You monster.", 1);
            messages.AddChoice($"You let {droneName} die. What did he ever do to you?", 1);
            messages.AddChoice($"{droneName} said goodbye. When he died.", 1);
            messages.AddChoice($"{droneName} died. You can repair him, but it won't be the same {droneName}.", 1);
            messages.AddChoice($"{droneName} has went to a junkyard in the country.", 1);
            messages.AddChoice($"{droneName} has went to a junkyard in the country.", 1);
            messages.AddChoice($"MISSION FAILURE! {droneName} has been lost.", 1);
            messages.AddChoice($"Well, {droneName} has been lost. Hope you're happy", 1);
            messages.AddChoice($"Well, {droneName} is gone. Hope you're happy", 1);
            messages.AddChoice($"{droneName} has gone and there shall never be another like him.", 1);
            messages.AddChoice($"RIP {droneName}, the goodest boy.", 1);
            messages.AddChoice($"RIP {droneName}.", 1);
            messages.AddChoice($"At least {droneName} didn't suffer.", 1);
            messages.AddChoice($"{droneName} deserved better.", 1);



            string deathMessage = "<style=cDeath><sprite name=\"Skull\" tint=1> " + messages.Evaluate(UnityEngine.Random.value) + " <sprite name=\"Skull\" tint=1></style>";

            return deathMessage;
        }

        IEnumerator DamageDrone(CharacterBody droneBody)
        {
            // seems health is active the very second is spawns
            yield return new WaitForSeconds(1);
            DamageInfo damageInfo = new DamageInfo
            {
                damage = droneBody.healthComponent.combinedHealth, // with nonlethal damage, it doesn't die
                position = droneBody.corePosition,
                force = Vector3.zero,
                damageColorIndex = DamageColorIndex.Default,
                crit = false,
                attacker = null,
                inflictor = null,
                damageType = (DamageType.NonLethal | DamageType.BypassArmor),
                procCoefficient = 0f,
                procChainMask = default(ProcChainMask)
            };
            droneBody.healthComponent.TakeDamage(damageInfo);
        }

    }
}
