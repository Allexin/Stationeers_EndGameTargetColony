using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Networks;
using UnityEngine;
using System.Collections;

namespace EndGameTargetColony
{
    [BepInPlugin("basovav.endgametargetcolony", "EndGame Target Colony", "1.0.0")]
    public class EndGameTargetColonyMod : BaseUnityPlugin
    {
        private static EndGameTargetColonyMod Instance;
        public static ConfigEntry<float> CloningDuration;
        
        private void Awake()
        {
            Instance = this;
            
            // Конфигурируемый параметр для времени клонирования (по умолчанию 60 секунд)
            // Это первый этап - создание базовых существ для НПЦ системы
            CloningDuration = Config.Bind("NPCCreation", "CloningDuration", 60f, 
                "Duration in seconds for NPC creation process (Phase 1: Animal cloning)");
            
            var harmony = new Harmony("com.stationeers.endgametargetcolony");
            harmony.PatchAll();
            
            Logger.LogInfo("EndGame Target Colony Mod loaded! Phase 1: NPC Creation System");
        }
        
        public static EndGameTargetColonyMod GetInstance()
        {
            return Instance;
        }
    }
}