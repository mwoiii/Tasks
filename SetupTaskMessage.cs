using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace Tasks
{
    class SetupTaskMessage : INetMessage
    {
        TaskInfo taskInfo;

        public SetupTaskMessage()
        { }
        public SetupTaskMessage(TaskInfo taskInfo)
        {
            this.taskInfo = taskInfo;
        }

        public void Deserialize(NetworkReader reader)
        {
            // is this legal?
            taskInfo = new TaskInfo(reader);
        }

        public void OnReceived()
        {
            if(NetworkServer.active)
            {
                // don't run this on server
                // i'm not sure if I should run on server or not
                // player 0 is the server....
                //UnityEngine.Debug.Log("This is the host");
                //return;
            }
            //UnityEngine.Debug.Log("This is the client");
            TasksPlugin.instance.SetupTasksClient(taskInfo);
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(taskInfo);
        }
    }

    class TaskCompletionMessage : INetMessage
    {
        int task;
        int playerNum;

        public TaskCompletionMessage() { }
        public TaskCompletionMessage(int task, int playerNum)
        {
            this.task = task;
            this.playerNum = playerNum;
        }

        public void Deserialize(NetworkReader reader)
        {
            task = reader.ReadInt32();
            playerNum = reader.ReadInt32();
            
        }

        public void OnReceived()
        {
            if (NetworkServer.active)
            {
                // don't run this on server
                //return;
            }
            TasksPlugin.instance.TaskCompletionClient(task, playerNum);
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(task);
            writer.Write(playerNum);
        }
    }

    class UpdateProgressMessage : INetMessage
    {
        ProgressInfo progressInfo;
        int playerNum;

        public UpdateProgressMessage() { }
        public UpdateProgressMessage(ProgressInfo progressInfo, int playerNum)
        {
            this.progressInfo = progressInfo;
            this.playerNum = playerNum;
        }

        public void Deserialize(NetworkReader reader)
        {
            int taskIndex = reader.ReadInt32();
            int myProgress = reader.ReadInt32();
            int rivalProgress = reader.ReadInt32();
            progressInfo = new ProgressInfo(taskIndex, myProgress, rivalProgress);
            playerNum = reader.ReadInt32();
        }

        public void OnReceived()
        {
            if (NetworkServer.active)
            {
                // don't run this on server
                //return;
            }
            TasksPlugin.instance.UpdateProgressClient(progressInfo, playerNum);
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(progressInfo.taskIndex);
            writer.Write((int)progressInfo.GetMyProgress() * 10);
            writer.Write((int)progressInfo.GetRivalProgress() * 10);
            writer.Write(playerNum);
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
            writer.Write(reward.dronePath);
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
            string path = reader.ReadString();

            PickupIndex p = new PickupIndex(item);
            reward = new Reward((RewardType)type, p, numItems, temp, gold, xp, path);
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

        public TaskInfo(NetworkReader reader)
        {
            Deserialize(reader);
        }

        public override string ToString() => $"TaskInfo: {taskType}, {description}, {completed}, {index}/{total}";
    }

    public class ProgressInfo : MessageBase
    {
        public int taskIndex;
        int myProgress;
        int rivalProgress;

        public ProgressInfo(int _taskIndex, float _myProgress, float _rivalProgress)
        {
            taskIndex = _taskIndex;
            // can't serialize floats (or I don't know how)
            // so just convert from 0.0->1.0 to 00 to 10
            // Lose most decimal places, but didn't need them anyway
            myProgress = (int)(_myProgress * 10);
            rivalProgress = (int)(_rivalProgress * 10);
        }

        public ProgressInfo(int _taskIndex, int _myProgress, int _rivalProgress)
        {
            taskIndex = _taskIndex;
            myProgress = _myProgress;
            rivalProgress = _rivalProgress;
        }

        public float GetMyProgress()
        {
            // convert back to floats
            return myProgress / 10f;
        }

        public float GetRivalProgress()
        {
            return rivalProgress / 10f;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(taskIndex);
            writer.Write(myProgress);
            writer.Write(rivalProgress);
        }

        public override void Deserialize(NetworkReader reader)
        {
            taskIndex = reader.ReadInt32();
            myProgress = reader.ReadInt32();
            rivalProgress = reader.ReadInt32();
        }
    }
}
