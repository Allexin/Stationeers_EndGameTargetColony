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
            public bool hasCompleted = false; // Флаг что клонирование уже завершено
            public float maxDuration;
            public float lastUpdateTime = 0f;
            
            public CloningState()
            {
                maxDuration = EndGameTargetColonyMod.CloningDuration.Value;
                timer = maxDuration; // Изначально таймер равен максимуму (0% прогресса)
                lastUpdateTime = Time.time;
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
                Debug.Log("CryoTube patch: Initialized new cloning state");
            }
            
            var state = cloningStates[__instance];
            
            // Всегда обновляем время для корректного расчета deltaTime
            float currentTime = Time.time;
            float deltaTime = currentTime - state.lastUpdateTime;
            state.lastUpdateTime = currentTime;
            
            // Проверяем условия для клонирования
            bool canClone = CheckCloningConditions(__instance);
            
            // Логируем состояние каждые 5 секунд для отладки
            if (Time.fixedTime % 5f < Time.fixedDeltaTime)
            {
                Debug.Log($"CryoTube state: canClone={canClone}, isCloning={state.isCloning}, hasCompleted={state.hasCompleted}, timer={state.timer:F1}");
            }
            
            if (canClone && !state.hasCompleted)
            {
                if (!state.isCloning)
                {
                    // Начинаем клонирование
                    state.isCloning = true;
                    state.timer = state.maxDuration;
                }
                
                // Уменьшаем таймер на реальное время прошедшее с последнего вызова
                // Ограничиваем deltaTime чтобы избежать больших скачков
                float clampedDeltaTime = Mathf.Min(deltaTime, 1f);
                state.timer -= clampedDeltaTime;
                
                // Не даем таймеру уйти в отрицательные значения
                state.timer = Mathf.Max(state.timer, 0f);
                
                if (state.timer <= 0f && !state.hasCompleted)
                {
                    // Клонирование завершено - сначала сбрасываем состояние, потом создаем курицу
                    state.hasCompleted = true; // Помечаем что клонирование завершено
                    ResetCloning(state);
                    CompleteCloning(__instance);
                }
            }
            else if (!canClone)
            {
                // Условия не выполнены - полностью сбрасываем состояние
                state.isCloning = false;
                state.hasCompleted = false; // Сбрасываем флаг завершения
                state.maxDuration = EndGameTargetColonyMod.CloningDuration.Value;
                state.timer = state.maxDuration; // Сбрасываем на 0% прогресса
                state.lastUpdateTime = Time.time;
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
            
            try
            {
                // Создаем базовое существо (курицу) как первый этап НПЦ системы
                NPCSpawner.SpawnNPC(cryoTube.transform.position);
                
                // Открываем дверь после завершения клонирования
                if (!cryoTube.IsOpen)
                {
                    // Пытаемся найти и вызвать метод для открытия двери
                    var openField = typeof(CryoTube).GetField("Open", BindingFlags.Public | BindingFlags.Instance);
                    if (openField != null)
                    {
                        var openSwitch = openField.GetValue(cryoTube);
                        if (openSwitch != null)
                        {
                            // Устанавливаем состояние "включено" для переключателя открытия
                            var onProperty = openSwitch.GetType().GetProperty("On");
                            if (onProperty != null)
                            {
                                onProperty.SetValue(openSwitch, true);
                                Debug.Log("CryoTube door opened after cloning completion");
                            }
                        }
                    }
                    
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in CompleteCloning: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }
        
        private static void ResetCloning(CloningState state)
        {
            state.isCloning = false;
            state.maxDuration = EndGameTargetColonyMod.CloningDuration.Value; // Обновляем из конфига
            state.timer = state.maxDuration; // После завершения таймер сбрасывается на максимум (0% прогресса)
            state.lastUpdateTime = Time.time; // Обновляем время последнего обновления
            // НЕ сбрасываем hasCompleted - это предотвратит повторное клонирование
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
            // Всегда показываем таймер для отладки
            float progress = ((state.maxDuration - state.timer) / state.maxDuration) * 100f;
            progress = Mathf.Clamp(progress, 0f, 100f);
            
            string baseStatus = $"Timer: {state.timer:F1}s Progress: {progress:F0}%";
            
            if (state.hasCompleted)
            {
                return baseStatus + " - Completed";
            }
            
            if (state.isCloning)
            {
                return baseStatus + " - Creating NPC";
            }
            
            // Проверяем условия и показываем что не так
            if (!cryoTube.Powered)
                return baseStatus + " - Not powered";
            
            if (cryoTube.IsOpen)
                return baseStatus + " - Door is open";
            
            // Проверяем что внутри нет сущностей
            if (cryoTube.Slots != null && cryoTube.Slots.Count > 0)
            {
                var sleeperSlot = cryoTube.Slots[0];
                if (sleeperSlot != null && sleeperSlot.Get() != null)
                    return baseStatus + " - Occupied";
            }
            
            return baseStatus + " - Ready";
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