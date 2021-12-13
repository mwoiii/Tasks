using R2API.Networking.Interfaces;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace Tasks
{

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

    public class TaskCompletionInfo : MessageBase
    {
        public int taskType;
        public string winnerName;

        public TaskCompletionInfo(int _taskType, string _winnerName)
        {
            taskType = _taskType;
            winnerName = _winnerName;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(taskType);
            writer.Write(winnerName);
        }

        public override void Deserialize(NetworkReader reader)
        {
            taskType = reader.ReadInt32();
            winnerName = reader.ReadString();
        }
    }
}
