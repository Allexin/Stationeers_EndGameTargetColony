using UnityEngine;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Networking;
using Assets.Scripts;
using System.Reflection;

namespace EndGameTargetColony
{
    public static class NPCSpawner
    {
        public static void SpawnNPC(Vector3 position)
        {
            try
            {
                Debug.Log("SpawnNPC called - starting spawn process");
                
                // Проверяем что мы на сервере (как в FertilizedEgg.Hatch)
                if (!GameManager.RunSimulation)
                {
                    Debug.LogWarning("Cannot spawn NPC: GameManager.RunSimulation is false");
                    return;
                }
                
                Debug.Log("GameManager.RunSimulation check passed");
                
                // Спавним FertilizedEgg который вылупится через 5 секунд
                DynamicThing eggPrefab = FindFertilizedEggPrefab();
                
                if (eggPrefab != null)
                {
                    Debug.Log($"FertilizedEgg prefab found: {eggPrefab.name}");
                    
                    // Создаем FertilizedEgg в мире
                    Vector3 spawnPosition = FindSafeSpawnPosition(position);
                    
                    Debug.Log($"Spawn position calculated: {spawnPosition}");
                    
                    // Используем OnServer.CreateOld как в FertilizedEgg.Hatch()
                    Debug.Log("About to call OnServer.CreateOld...");
                    DynamicThing eggObject = OnServer.CreateOld(eggPrefab, spawnPosition, Quaternion.identity, 0uL);
                    
                    // Настраиваем яйцо для быстрого вылупления
                    if (eggObject != null)
                    {
                        FertilizedEgg eggComponent = eggObject.GetComponent<FertilizedEgg>();
                        if (eggComponent != null)
                        {
                            // Устанавливаем время вылупления на 5 секунд
                            eggComponent.HatchTime = 5f;
                            Debug.Log("Set HatchTime to 5 seconds");
                            
                            // Принудительно делаем яйцо жизнеспособным
                            // Используем рефлексию для установки _viable = true
                            var viableField = typeof(FertilizedEgg).GetField("_viable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (viableField != null)
                            {
                                viableField.SetValue(eggComponent, true);
                                Debug.Log("Set egg as viable");
                            }
                        }
                    }
                    
                    Debug.Log($"NPC Phase 1: FertilizedEgg spawned successfully at position: {spawnPosition}");
                }
                else
                {
                    Debug.LogError("FertilizedEgg prefab not found!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error spawning NPC: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }
        
        private static DynamicThing FindFertilizedEggPrefab()
        {
            try
            {
                // Ищем префаб FertilizedEgg
                
                // Поиск через Prefab.Find по имени
                var eggPrefab = Prefab.Find("FertilizedEgg");
                if (eggPrefab != null)
                {
                    Debug.Log("Found FertilizedEgg prefab through Prefab.Find");
                    return eggPrefab as DynamicThing;
                }
                
                // Альтернативный поиск среди всех DynamicThing префабов
                foreach (var prefab in DynamicThing.DynamicThingPrefabs)
                {
                    if (prefab.name.Contains("FertilizedEgg") || prefab.name.Contains("Egg"))
                    {
                        Debug.Log($"Found Egg prefab in DynamicThingPrefabs: {prefab.name}");
                        return prefab;
                    }
                }
                
                Debug.LogWarning("FertilizedEgg prefab not found in any lookup method");
                return null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error finding FertilizedEgg prefab: {e.Message}");
                return null;
            }
        }
        

        
        private static Vector3 FindSafeSpawnPosition(Vector3 basePosition)
        {
            // Ищем безопасную позицию для спавна рядом с CryoTube
            Vector3 spawnPosition = basePosition;
            
            // Проверяем позицию на 1 метр вперед от CryoTube
            Vector3[] offsets = {
                Vector3.forward,
                Vector3.back,
                Vector3.left,
                Vector3.right,
                Vector3.forward + Vector3.right,
                Vector3.forward + Vector3.left,
                Vector3.back + Vector3.right,
                Vector3.back + Vector3.left
            };
            
            foreach (var offset in offsets)
            {
                Vector3 testPosition = basePosition + offset;
                
                // Проверяем что позиция свободна
                if (IsPositionSafe(testPosition))
                {
                    return testPosition;
                }
            }
            
            // Если не нашли безопасную позицию, используем базовую
            return basePosition + Vector3.up * 0.5f;
        }
        
        private static bool IsPositionSafe(Vector3 position)
        {
            // Простая проверка - пока что всегда возвращаем true
            // В будущем можно добавить более сложную логику проверки препятствий
            return true;
        }
        

    }
}