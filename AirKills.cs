using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace Tasks
{
    class AirKills : Task
    {

        // so doing just this doesn't seem to work
        //protected static new string thisClass = "AIR_KILLS_";
        //  /Logbook/LOGBOOK_CATEGORY_ACHIEVEMENTS/SOLRUN_TEMP_ACHIEVEMENTS_KILL_BEETLE_ACHIEVEMENT_NAME /Logbook/LOGBOOK_CATEGORY_ACHIEVEMENTS/SOLRUN_TASKS_BASE_TASK_ACHIEVEMENT_NAME

        public override string AchievementIdentifier { get; } = "SOLRUN_TASKS_AIRKILLS_ACHIEVEMENT_ID"; // delete me? Pretty sure
        public override string UnlockableIdentifier { get; } = "SOLRUN_TASKS_AIRKILLS_REWARD_ID"; // Delete me. Maybe not
        // I think all this does is hide it in the log until you have the prereq. You could still complete it (except most prereqs seem to be characters)
        public override string PrerequisiteUnlockableIdentifier { get; } = "";
        public override string AchievementNameToken { get; } = "SOLRUN_TASKS_AIRKILLS_ACHIEVEMENT_NAME"; // Fine to have in the XML
        public override string AchievementDescToken { get; } = "Get 3 kills whilst airborne"; // plain English
        public override string UnlockableNameToken { get; } = ""; // plain English

        //protected override int _id { get; } = 1;
        protected override TaskType type { get; } = TaskType.AirKills;
        protected override string name { get; } = "Air Kills";

        int[] kills;
        int killsNeeded = 3;

        //delegate void killDelegate(DamageReport damageReport);

        List<CharacterMotor.HitGroundDelegate> groundDelegateList = new List<CharacterMotor.HitGroundDelegate>();

        protected override void SetHooks(int numPlayers)
        {
            //Language.currentLanguage.SetStringByToken(AchievementNameToken, "Air Kills");
            Chat.AddMessage($"Set Hooks in AirKills. {numPlayers} players");
            

            base.SetHooks(numPlayers);
            kills = new int[numPlayers];

            //kills = 0;

            //killDelegate killMethod = OnKill;
            //CmdSetHooks(killMethod);

            GlobalEventManager.onCharacterDeathGlobal += OnKill;
            // if(server)
            //if(NetworkServer.active)
            //GlobalEventManager.onCharacterDeathGlobal += RpcOnKill;
            // if networkServer.notActive return;
            /*
            if(ownerCached is null)
            {
                Chat.AddMessage("Owner is null");
            }
            if (ownerCached.localUser is null)
            {
                Chat.AddMessage("localUser is null");
            }
            else
            {
                Chat.AddMessage($"localUser: {ownerCached.localUser} Id: {ownerCached.localUser.id}");
            }
            if (ownerCached.localUser.cachedBody is null)
            {
                Chat.AddMessage("body is null");
            }
            if (ownerCached.localUser.cachedBody.characterMotor is null)
            {
                Chat.AddMessage("Motor is null");
            }
            */
            // They are
            //Chat.AddMessage($"Owners are still the same: {ownerCached == owner}");
            
            //ownerCached.localUser.cachedBody.characterMotor.onHitGround += OnLanding;
            
            for (int i = 0; i < totalNumberPlayers; i++)
            {
                CharacterMaster current = TasksPlugin.GetPlayerCharacterMaster(i);
                if (current is null) break;
                if (current.GetBody() is null) break;
                if (current.GetBody().characterMotor is null) break;

                //Chat.AddMessage($"Set hooks for player {i}");
                // is it a weird timing thing?
                // like when onHitGround eventually gets called, it just finds what i was last instead of what it was when this was first added. Like lazy evaluation
                // I think I was right. When I set i=5 after this, when I hit the ground, it said player 6 did it (which is what i was last)
                // Neat. This fixes it.
                // why: https://answers.unity.com/questions/908847/passing-a-temporary-variable-to-add-listener.html
                // something something scope. a for loop is compiled into int i=0; while() {}
                // so the i isn't inside the loop like this temp is.
                int tempInt = i;

                //Chat.AddMessage($"Hook Status. Player {tempInt} CharacterMaster: {TasksPlugin.GetPlayerCharacterMaster(i)} Body: {TasksPlugin.GetPlayerCharacterMaster(i).GetBody()} Motor: {TasksPlugin.GetPlayerCharacterMaster(i).GetBody().characterMotor}");

                // to get it to remove itself, I need to store the delegate in a list
                CharacterMotor.HitGroundDelegate myDel = (ref CharacterMotor.HitGroundInfo _) => PlayerHitGround(tempInt);
                groundDelegateList.Add(myDel);
                current.GetBody().characterMotor.onHitGround += groundDelegateList[tempInt];
                // this version works, but I can't unsub it
                //TasksPlugin.GetPlayerCharacterMaster(i).GetBody().characterMotor.onHitGround += (ref CharacterMotor.HitGroundInfo _) => PlayerHitGround(tempInt);
            }
        }

        protected override void Unhook()
        {
            GlobalEventManager.onCharacterDeathGlobal -= OnKill;

            for (int i = 0; i < totalNumberPlayers; i++)
            {
                CharacterMaster current = TasksPlugin.GetPlayerCharacterMaster(i);
                if (current is null) break;
                if (current.GetBody() is null) break;
                if (current.GetBody().characterMotor is null) break;
                // I don't think I need to worry about unsubscribing when one of the players dies
                // Because onHitGround is probably destroyed/reset anyway.

                // delegate list is created when it's hooked up. This might break if unhook is called before setHooks
                int tempInt = i;
                //Chat.AddMessage($"Unhook Status. Player {tempInt} CharacterMaster: {TasksPlugin.GetPlayerCharacterMaster(i)} Body: {TasksPlugin.GetPlayerCharacterMaster(i).GetBody()} Motor: {TasksPlugin.GetPlayerCharacterMaster(i).GetBody().characterMotor}");
                //Chat.AddMessage()

                // seems like this breaks if one of the players is dead
                current.GetBody().characterMotor.onHitGround -= groundDelegateList[tempInt];
            }

            base.Unhook();
        }

        protected override bool IsComplete(int playerNum)
        {
            return kills[playerNum] >= killsNeeded;
        }

        public void OnKill(DamageReport damageReport)
        {
            if (damageReport is null) return;
            if (damageReport.attackerMaster is null) return;
            if (damageReport.attackerMaster.playerCharacterMasterController is null) return;

            //if (damageReport.victimMaster is null) return;
            //Chat.AddMessage(String.Format("Killer: {0} Me: {1} Victim: {2}", damageReport.attackerMaster.ToString(), damageReport.victimMaster.ToString(), ownerCached.localUser.cachedMaster.ToString()));
            //[Info: Unity Log] Killer: CommandoMaster(Clone)(RoR2.CharacterMaster) Me: LemurianMaster(Clone)(RoR2.CharacterMaster) Victim: CommandoMaster(Clone)(RoR2.CharacterMaster)
            // Did I kill it?
            int playerNum = TasksPlugin.GetPlayerNumber(damageReport.attackerMaster);
            
            // seems easier than reworking Airborne()
            if(!damageReport.attackerMaster.GetBody().characterMotor.isGrounded)
            {
                kills[playerNum]++;
                if(IsComplete(playerNum))
                {
                    Chat.AddMessage($"Player {playerNum} Completed AirKills");
                    CompleteTask(playerNum);
                    // What about getting 2nd place?
                    ResetAllKills();
                }
            }
        }

        void PlayerHitGround(int playerNum)
        {
            //Chat.AddMessage($"Player {playerNum} landed");
            kills[playerNum] = 0;
        }

        void ResetAllKills()
        {
            for (int i = 0; i < kills.Length; i++)
            {
                kills[i] = 0;
            }
        }
    }
}
