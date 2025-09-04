using HarmonyLib;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace EndGameTargetColony
{
    [HarmonyPatch(typeof(CryoTube))]
    public class CryoTubePatch
    {
        // Словарь для хранения состояния клонирования каждой CryoTube
        private static Dictionary<CryoTube, CloningState> cloningStates = new Dictionary<CryoTube, CloningState>();
        
        private const int REQUIRED_SETTING = 5; // Константа для Setting
        
        private class CloningState
        {
            public float timer = 0f;
            public bool isCloning = false;
            public float maxDuration;
            
            public CloningState()
            {
                maxDuration = EndGameTargetColonyMod.CloningDuration.Value;
            }
        }
        
        [HarmonyPatch("OnAtmosphericTick")]
        [HarmonyPostfix]
        public static void OnAtmosphericTick_Postfix(CryoTube __instance)
        {
            // Инициализируем состояние если его нет
            if (!cloningStates.ContainsKey(__instance))
            {
                cloningStates[__instance] = new CloningState();
            }
            
            var state = cloningStates[__instance];
            
            // Проверяем условия для клонирования
            bool canClone = CheckCloningConditions(__instance);
            
            if (canClone)
            {
                if (!state.isCloning)
                {
                    // Начинаем клонирование
                    state.isCloning = true;
                    state.timer = state.maxDuration;
                }
                
                // Уменьшаем таймер
                state.timer -= Time.fixedDeltaTime;
                
                if (state.timer <= 0f)
                {
                    // Клонирование завершено - создаем курицу
                    CompleteCloning(__instance);
                    ResetCloning(state);
                }
            }
            else
            {
                // Условия не выполнены - сбрасываем таймер
                if (state.isCloning)
                {
                    ResetCloning(state);
                }
            }
            
            // Обновляем дисплей
            UpdateDisplay(__instance, state);
        }
        
        private static bool CheckCloningConditions(CryoTube cryoTube)
        {
            // Проверяем все условия:
            // 1. Включен (Powered)
            // 2. Закрыт (!IsOpen) - дверь должна быть закрыта
            // 3. Пустой (нет сущностей внутри)
            
            if (!cryoTube.Powered) return false;
            if (cryoTube.IsOpen) return false; // Дверь должна быть закрыта
            
            // Проверяем что внутри нет сущностей
            if (cryoTube.Slots != null && cryoTube.Slots.Count > 0)
            {
                var sleeperSlot = cryoTube.Slots[0]; // SleeperSlot
                if (sleeperSlot != null && sleeperSlot.Get() != null)
                    return false;
            }
            
            return true;
        }
        
        private static void CompleteCloning(CryoTube cryoTube)
        {
            Debug.Log("NPC creation completed! Phase 1: Animal spawning");
            
            // Создаем базовое существо (курицу) как первый этап НПЦ системы
            NPCSpawner.SpawnNPC(cryoTube.transform.position);
        }
        
        private static void ResetCloning(CloningState state)
        {
            state.isCloning = false;
            state.timer = 0f;
            state.maxDuration = EndGameTargetColonyMod.CloningDuration.Value; // Обновляем из конфига
        }
        
        private static void UpdateDisplay(CryoTube cryoTube, CloningState state)
        {
            // Пока что просто логируем прогресс
            if (state.isCloning)
            {
                float progress = ((state.maxDuration - state.timer) / state.maxDuration) * 100f;
                if (Time.fixedTime % 5f < Time.fixedDeltaTime) // Логируем каждые 5 секунд
                {
                    Debug.Log($"NPC Creation Progress: {progress:F0}%");
                }
            }
        }
        
        [HarmonyPatch("GetPassiveTooltip")]
        [HarmonyPostfix]
        public static void GetPassiveTooltip_Postfix(CryoTube __instance, ref PassiveTooltip __result)
        {
            // Добавляем информацию о клонировании в тултип
            if (!cloningStates.ContainsKey(__instance))
            {
                cloningStates[__instance] = new CloningState();
            }
            
            var state = cloningStates[__instance];
            string cloningStatus = GetCloningStatusText(__instance, state);
            
            // Добавляем статус клонирования к существующему тултипу
            if (!string.IsNullOrEmpty(__result.Extended))
            {
                __result.Extended += "\n" + cloningStatus;
            }
            else
            {
                __result.Extended = cloningStatus;
            }
        }
        
        private static string GetCloningStatusText(CryoTube cryoTube, CloningState state)
        {
            if (state.isCloning)
            {
                float progress = ((state.maxDuration - state.timer) / state.maxDuration) * 100f;
                return $"Cloning Status: Creating NPC ({progress:F0}%)";
            }
            
            // Проверяем условия и показываем что не так
            if (!cryoTube.Powered)
                return "Cloning Status: Not powered";
            
            if (cryoTube.IsOpen)
                return "Cloning Status: Door is open";
            
            // Проверяем что внутри нет сущностей
            if (cryoTube.Slots != null && cryoTube.Slots.Count > 0)
            {
                var sleeperSlot = cryoTube.Slots[0];
                if (sleeperSlot != null && sleeperSlot.Get() != null)
                    return "Cloning Status: Occupied";
            }
            
            return "Cloning Status: Ready";
        }
        
        [HarmonyPatch("OnDestroy")]
        [HarmonyPrefix]
        public static void OnDestroy_Prefix(CryoTube __instance)
        {
            // Очищаем состояние при уничтожении CryoTube
            if (cloningStates.ContainsKey(__instance))
            {
                cloningStates.Remove(__instance);
            }
        }
    }
}