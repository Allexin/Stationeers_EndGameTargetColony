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
                Debug.Log($"CryoTube state: canClone={canClone}, isCloning={state.isCloning}, timer={state.timer:F1}");
            }
            
            if (canClone)
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
                
                if (state.timer <= 0f)
                {
                    // Клонирование завершено - создаем курицу и выключаем трубу
                    CompleteCloning(__instance);
                    ResetCloning(state);
                }
            }
            else if (!canClone)
            {
                // Условия не выполнены - полностью сбрасываем состояние
                state.isCloning = false;
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
            Debug.Log("NPC creation completed! Phase 1: Opening door and preparing spawn");
            
            try
            {
                // Сначала открываем дверь и выключаем трубу
                TryOpenCryoTubeDoor(cryoTube);
                TryPowerOffCryoTube(cryoTube);
                
                // Затем запускаем отложенный спавн курицы через 1 секунду
                EndGameTargetColonyMod.GetInstance().StartCoroutine(DelayedSpawnCoroutine(cryoTube.transform.position));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in CompleteCloning: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }
        
        private static System.Collections.IEnumerator DelayedSpawnCoroutine(Vector3 spawnPosition)
        {
            Debug.Log("DelayedSpawnCoroutine: Waiting 1 second before spawning...");
            
            // Ждем 1 секунду
            yield return new WaitForSeconds(1f);
            
            Debug.Log("DelayedSpawnCoroutine: Now spawning NPC...");
            
            try
            {
                // Создаем базовое существо (курицу) как первый этап НПЦ системы
                NPCSpawner.SpawnNPC(spawnPosition);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in DelayedSpawnCoroutine: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }
        
        private static void TryPowerOffCryoTube(CryoTube cryoTube)
        {
            try
            {
                Debug.Log($"TryPowerOffCryoTube: Current Powered state = {cryoTube.Powered}");
                
                if (!cryoTube.Powered)
                {
                    Debug.Log("CryoTube is already powered off");
                    return;
                }
                
                // Метод 1: Попробуем напрямую через свойство (если доступно для записи)
                try
                {
                    var poweredProperty = typeof(CryoTube).GetProperty("Powered", BindingFlags.Public | BindingFlags.Instance);
                    if (poweredProperty != null && poweredProperty.CanWrite)
                    {
                        poweredProperty.SetValue(cryoTube, false);
                        Debug.Log("Method 1: Successfully set Powered = false");
                        return;
                    }
                    else
                    {
                        Debug.Log("Method 1: Powered property not writable or not found");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Method 1 failed: {e.Message}");
                }
                
                // Метод 2: Поиск через все поля
                var allFields = typeof(CryoTube).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                Debug.Log($"Found {allFields.Length} fields in CryoTube");
                
                foreach (var field in allFields)
                {
                    Debug.Log($"Field: {field.Name}, Type: {field.FieldType}");
                    if (field.Name.ToLower().Contains("onoff") || field.Name.ToLower().Contains("power"))
                    {
                        Debug.Log($"Found potential power field: {field.Name}");
                        var value = field.GetValue(cryoTube);
                        if (value != null)
                        {
                            Debug.Log($"Field {field.Name} value type: {value.GetType()}");
                            var onProperty = value.GetType().GetProperty("On");
                            if (onProperty != null && onProperty.CanWrite)
                            {
                                onProperty.SetValue(value, false);
                                Debug.Log($"Method 2: Successfully turned off via {field.Name}.On");
                                return;
                            }
                        }
                    }
                }
                
                Debug.LogWarning("Failed to find a way to power off CryoTube");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in TryPowerOffCryoTube: {e.Message}");
            }
        }
        
        private static void TryOpenCryoTubeDoor(CryoTube cryoTube)
        {
            try
            {
                Debug.Log($"TryOpenCryoTubeDoor: Current IsOpen state = {cryoTube.IsOpen}");
                
                if (cryoTube.IsOpen)
                {
                    Debug.Log("CryoTube door is already open");
                    return;
                }
                
                // Метод 1: Попробуем напрямую через свойство (если доступно для записи)
                try
                {
                    var isOpenProperty = typeof(CryoTube).GetProperty("IsOpen", BindingFlags.Public | BindingFlags.Instance);
                    if (isOpenProperty != null && isOpenProperty.CanWrite)
                    {
                        isOpenProperty.SetValue(cryoTube, true);
                        Debug.Log("Method 1: Successfully set IsOpen = true");
                        return;
                    }
                    else
                    {
                        Debug.Log("Method 1: IsOpen property not writable or not found");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Method 1 failed: {e.Message}");
                }
                
                // Метод 2: Поиск через все поля
                var allFields = typeof(CryoTube).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                foreach (var field in allFields)
                {
                    if (field.Name.ToLower().Contains("open") || field.Name.ToLower().Contains("door"))
                    {
                        Debug.Log($"Found potential door field: {field.Name}");
                        var value = field.GetValue(cryoTube);
                        if (value != null)
                        {
                            Debug.Log($"Field {field.Name} value type: {value.GetType()}");
                            var onProperty = value.GetType().GetProperty("On");
                            if (onProperty != null && onProperty.CanWrite)
                            {
                                onProperty.SetValue(value, true);
                                Debug.Log($"Method 2: Successfully opened via {field.Name}.On");
                                return;
                            }
                        }
                    }
                }
                
                Debug.LogWarning("Failed to find a way to open CryoTube door");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in TryOpenCryoTubeDoor: {e.Message}");
            }
        }
        
        private static void ResetCloning(CloningState state)
        {
            state.isCloning = false;
            state.maxDuration = EndGameTargetColonyMod.CloningDuration.Value; // Обновляем из конфига
            state.timer = state.maxDuration; // После завершения таймер сбрасывается на максимум (0% прогресса)
            state.lastUpdateTime = Time.time; // Обновляем время последнего обновления
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
            
            if (state.isCloning)
            {
                return baseStatus + " - Cloning";
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