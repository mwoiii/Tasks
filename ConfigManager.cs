using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tasks
{
    class ConfigManager
    {
        public static ConfigManager instance;

        ConfigFile config;

        public static ConfigEntry<int> TasksPerPlayer { get; set; }
        public static ConfigEntry<int> AdditionalTasks { get; set; }


        public static ConfigEntry<float> AirKillsWeight { get; set; }
        public static ConfigEntry<float> DamageMultipleWeight { get; set; }
        public static ConfigEntry<float> DamageInTimeWeight { get; set; }
        public static ConfigEntry<float> StayInAirWeight { get; set; }
        public static ConfigEntry<float> BiggestHitWeight { get; set; }
        public static ConfigEntry<float> MostDistanceWeight { get; set; }
        public static ConfigEntry<float> PreonEventWeight { get; set; }
        public static ConfigEntry<float> FarthestAwayWeight { get; set; }
        public static ConfigEntry<float> FailShrineWeight { get; set; }
        public static ConfigEntry<float> OpenChestsWeight { get; set; }
        public static ConfigEntry<float> StartTeleWeight { get; set; }
        public static ConfigEntry<float> UsePrintersWeight { get; set; }
        public static ConfigEntry<float> OrderedSkillsWeight { get; set; }
        public static ConfigEntry<float> BadSkillWeight { get; set; }
        public static ConfigEntry<float> BabyDroneWeight { get; set; }
        public static ConfigEntry<float> DieWeight { get; set; }
        public static ConfigEntry<float> FindLockboxWeight { get; set; }
        public static ConfigEntry<float> HealingItemWeight { get; set; }
        public static ConfigEntry<float> NoJumpWeight { get; set; }
        public static ConfigEntry<float> VeryBestWeight { get; set; }
        public static ConfigEntry<float> FewestElitesWeight { get; set; }
        public static ConfigEntry<float> GetLuckyWeight { get; set; }
        public static ConfigEntry<float> GetLowWeight { get; set; }
        public static ConfigEntry<float> KillStreakWeight { get; set; }
        public static ConfigEntry<float> QuickDrawWeight { get; set; }










        public ConfigManager()
        {
            // how do I delete if an instance already exists? Should the class be static?
            // it probably won't matter either way. I won't create another configManager
            if (instance != null)
                return;
            instance = this;
        }

        public void Awake()
        {

            SetupWeights();
        }

        public void SetConfigFile(ConfigFile c)
        {
            config = c;
        }
        
        void SetupWeights()
        {
            TasksPerPlayer = config.Bind<int>(
                "TaskOptions",
                "TasksPerPlayer",
                1,
                "Number of tasks per player. Number of tasks: (TasksPerPlayer * number of player) + AdditionalTasks"
                );
            AdditionalTasks = config.Bind<int>(
                "TaskOptions",
                "AdditionalTasks",
                2,
                "Extra tasks. Number of tasks: (TasksPerPlayer * number of player) + AdditionalTasks"
                );

            AirKillsWeight = config.Bind<float>(
                "TaskWeights",
                "AirKills",
                1.5f,
                "Relative weight of this task. Bigger number means more likely to roll task. 0 for no chance."
                );
            DamageMultipleWeight = config.Bind<float>(
                "TaskWeights",
                "DamageMultiple",
                2.0f,
                "Relative weight of this task."
                );
            DamageInTimeWeight = config.Bind<float>(
                "TaskWeights",
                "DamageInTime",
                2.0f,
                "Relative weight of this task. Do x damage within y seconds"
                );
            StayInAirWeight = config.Bind<float>(
                "TaskWeights",
                "StayInAir",
                1.5f,
                "Relative weight of this task."
                );
            BiggestHitWeight = config.Bind<float>(
                "TaskWeights",
                "BiggestHit",
                2.0f,
                "Relative weight of this task."
                );
            MostDistanceWeight = config.Bind<float>(
                "TaskWeights",
                "MostDistance",
                1.0f,
                "Relative weight of this task."
                );
            PreonEventWeight = config.Bind<float>(
                "TaskWeights",
                "PreonEvent",
                1.0f,
                "Relative weight of this task."
                );
            FarthestAwayWeight = config.Bind<float>(
                "TaskWeights",
                "FarthestAway",
                1.5f,
                "Relative weight of this task."
                );
            FailShrineWeight = config.Bind<float>(
                "TaskWeights",
                "FailShrine",
                1.0f,
                "Relative weight of this task."
                );
            OpenChestsWeight = config.Bind<float>(
                "TaskWeights",
                "OpenChests",
                2.0f,
                "Relative weight of this task."
                );
            StartTeleWeight = config.Bind<float>(
                "TaskWeights",
                "StartTele",
                2.0f,
                "Relative weight of this task."
                );
            UsePrintersWeight = config.Bind<float>(
                "TaskWeights",
                "UsePrinters",
                1.0f,
                "Relative weight of this task."
                );
            OrderedSkillsWeight = config.Bind<float>(
                "TaskWeights",
                "OrderedSkills",
                0.0f,
                "Relative weight of this task. Use abilities in left-to-right order. This task sucks."
                );
            BadSkillWeight = config.Bind<float>(
                "TaskWeights",
                "BadSkill",
                0.5f,
                "Relative weight of this task. Don't use utility skill"
                );
            BabyDroneWeight = config.Bind<float>(
                "TaskWeights",
                "BabyDrone",
                0.75f,
                "Relative weight of this task. Keep your drone alive"
                );
            DieWeight = config.Bind<float>(
                "TaskWeights",
                "Die",
                0.25f,
                "Relative weight of this task."
                );
            FindLockboxWeight = config.Bind<float>(
                "TaskWeights",
                "FindLockbox",
                2.0f,
                "Relative weight of this task."
                );
            HealingItemWeight = config.Bind<float>(
                "TaskWeights",
                "HealingItem",
                1.5f,
                "Relative weight of this task. Find a healing item"
                );
            NoJumpWeight = config.Bind<float>(
                "TaskWeights",
                "NoJump",
                1.0f,
                "Relative weight of this task."
                );
            VeryBestWeight = config.Bind<float>(
                "TaskWeights",
                "VeryBest",
                1.5f,
                "Relative weight of this task. Be the very best. Like no one ever was."
                );
            FewestElitesWeight = config.Bind<float>(
                "TaskWeights",
                "FewestElites",
                1.5f,
                "Relative weight of this task."
                );
            GetLuckyWeight = config.Bind<float>(
                "TaskWeights",
                "GetLucky",
                0.5f,
                "Relative weight of this task."
                );
            GetLowWeight = config.Bind<float>(
                "TaskWeights",
                "GetLow",
                1.5f,
                "Relative weight of this task."
                );
            KillStreakWeight = config.Bind<float>(
                "TaskWeights",
                "KillStreaks",
                1.5f,
                "Relative weight of this task."
                );
            QuickDrawWeight = config.Bind<float>(
                "TaskWeights",
                "QuickDraw",
                1.5f,
                "Relative weight of this task. Kill somehting that spawned 3s ago"
                );
        }

        public float GetTaskWeight(TaskType type)
        {
            switch(type)
            {
                case TaskType.AirKills:
                    return AirKillsWeight.Value;

                case TaskType.DamageMultiple:
                    return DamageMultipleWeight.Value;

                case TaskType.DamageInTime:
                    return DamageInTimeWeight.Value;

                case TaskType.StayInAir:
                    return StayInAirWeight.Value;

                case TaskType.BiggestHit:
                    return BiggestHitWeight.Value;

                case TaskType.MostDistance:
                    return MostDistanceWeight.Value;

                case TaskType.PreonEvent:
                    return PreonEventWeight.Value;

                case TaskType.FarthestAway:
                    return FarthestAwayWeight.Value;

                case TaskType.FailShrine:
                    return FailShrineWeight.Value;

                case TaskType.OpenChests:
                    return OpenChestsWeight.Value;

                case TaskType.StartTele:
                    return StartTeleWeight.Value;

                case TaskType.UsePrinters:
                    return UsePrintersWeight.Value;

                case TaskType.OrderedSkills:
                    return OrderedSkillsWeight.Value;

                case TaskType.BadSkill:
                    return BadSkillWeight.Value;

                case TaskType.BabyDrone:
                    return BabyDroneWeight.Value;

                case TaskType.Die:
                    return DieWeight.Value;

                case TaskType.FindLockbox:
                    return FindLockboxWeight.Value;

                case TaskType.HealingItem:
                    return HealingItemWeight.Value;

                case TaskType.NoJump:
                    return NoJumpWeight.Value;

                case TaskType.VeryBest:
                    return VeryBestWeight.Value;

                case TaskType.FewestElites:
                    return FewestElitesWeight.Value;

                case TaskType.GetLucky:
                    return GetLuckyWeight.Value;

                case TaskType.GetLow:
                    return GetLowWeight.Value;

                case TaskType.KillStreak:
                    return KillStreakWeight.Value;

                case TaskType.QuickDraw:
                    return QuickDrawWeight.Value;
            }
            return 1;
        }

        public int GetNumberOfTasks(int numPlayers)
        {
            return numPlayers * TasksPerPlayer.Value + AdditionalTasks.Value;
        }
    }
}
