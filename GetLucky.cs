using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace Tasks
{
    class GetLucky : Task
    {
        public override TaskType type { get; } = TaskType.GetLucky;

        protected override string name { get; } = "Get Lucky";

        bool active = false;

        public override bool CanActivate(int numPlayers)
        {
            return true; // numPlayers > 1;
        }

        public override string GetDescription()
        {
            return "Be the luckiest.";
        }

        public override string GetWinMessage(int winningPlayer)
        {
            return $"{GetStylizedName(winningPlayer)} completed {GetStylizedTaskName(name)} by getting {GetStylizedTaskWinStat("lucky")}.";
        }

        protected override void SetHooks(int numPlayers)
        {
            Debug.Log($"Set hooks in GetLucky. {numPlayers} players");
            base.SetHooks(numPlayers);

            active = true;
        }

        protected override void StageEnd()
        {
            base.StageEnd();

            Evaluate();
        }

        protected override void Unhook()
        {
            base.Unhook();
        }

        void Evaluate()
        {
            int r = UnityEngine.Random.Range(0, totalNumberPlayers); //[inclusive, exclusinve)
            if(totalNumberPlayers == 1)
            {
                r = UnityEngine.Random.Range(0, 4);
            }
            // should probably say something in chat
            // like "Rolling...."
            // wait 2 sec
            // "Congrats player 2!"
            if(TasksPlugin.instance)
                TasksPlugin.instance.StartCoroutine(RollEvent(r));
        }

        string GetRollMessage()
        {
            WeightedSelection<string> messages = new WeightedSelection<string>();
            messages.AddChoice("Rolling...",1);
            messages.AddChoice("Who's going to be the lucky one?", 1);
            messages.AddChoice("Feeling lucky?", 1);
            messages.AddChoice("Choosing the winner. Stand by.", 1);
            messages.AddChoice("Scratching the lottery ticket.", 1);
            messages.AddChoice("Rolling for the winner.", 1);
            messages.AddChoice("Flipping a coin. Call it in the air.", 1);
            messages.AddChoice("Rigging the draw. Please wait.", 1);


            string rollMessage = "<style=cEvent>"+ messages.Evaluate(UnityEngine.Random.value)+ "</style>";
            return rollMessage;
        }

        string GetWinFlavourMessage(string playerName)
        {
            WeightedSelection<string> messages = new WeightedSelection<string>();
            messages.AddChoice($"{playerName} won!", 1);
            messages.AddChoice($"{playerName} was the lucky one!", 1);
            messages.AddChoice($"Congrats, {playerName}.", 1);
            messages.AddChoice($"The winner is {playerName}.", 1);
            messages.AddChoice($"Your winner was {playerName}.", 1);
            messages.AddChoice($"Congrats, {playerName}.", 1);
            messages.AddChoice($"It's your lucky day, {playerName}.", 1);
            messages.AddChoice($"Enjoy your prize, {playerName}.", 1);
            messages.AddChoice($"{playerName} won, but it was rigged from the start.", 1);
            messages.AddChoice($"Everyone loses except for {playerName}.", 1);


            string winMessage = messages.Evaluate(UnityEngine.Random.value);
            return winMessage;
        }

        IEnumerator RollEvent(int playerNum)
        {
            
            ChatMessage.Send(GetRollMessage());
            yield return new WaitForSeconds(1.5f);
            // congrats ???
            //string name = Util.GetBestBodyNameColored(TasksPlugin.GetPlayerCharacterMaster(playerNum).gameObject);
            string name = "Nobody";

            if(totalNumberPlayers > 1 || playerNum == 0)
            {
                name = Util.GetBestMasterName(TasksPlugin.GetPlayerCharacterMaster(playerNum));

                CompleteTask(playerNum);
            }
            ChatMessage.Send(GetWinFlavourMessage(name));

        }
    }
}
