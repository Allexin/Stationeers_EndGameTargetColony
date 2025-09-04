using UnityEngine;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Networking;
using Assets.Scripts;
using System.Reflection;
using System.Collections;

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
                
                // Отложим создание объекта на следующий фрейм чтобы избежать проблем с графическим устройством
                EndGameTargetColonyMod.GetInstance().StartCoroutine(DelayedSpawnCoroutine(position));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error spawning NPC: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }
        
        private static IEnumerator DelayedSpawnCoroutine(Vector3 position)
        {
            Debug.Log("DelayedSpawnCoroutine started");
            
            // Ждем один фрейм
            yield return null;
            
            // Запускаем создание курицы (обработка ошибок внутри)
            yield return SpawnChickenCoroutine(position);
        }
        
        private static IEnumerator SpawnChickenCoroutine(Vector3 position)
        {
            Debug.Log("SpawnChickenCoroutine started");
            
            DynamicThing chickenPrefab = null;
            Vector3 spawnPosition = Vector3.zero;
            
            try
            {
                // Сначала попробуем найти префаб
                chickenPrefab = FindChickenPrefab();
                
                if (chickenPrefab == null)
                {
                    Debug.LogError("Chicken prefab not found!");
                    yield break;
                }
                
                Debug.Log($"Chicken prefab found: {chickenPrefab.name}");
                
                spawnPosition = FindSafeSpawnPosition(position);
                Debug.Log($"Spawn position calculated: {spawnPosition}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in SpawnChickenCoroutine setup: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                yield break;
            }
            
            // Попробуем разные методы создания объекта
            yield return TryCreateChickenMethods(chickenPrefab, spawnPosition);
        }
        
        private static IEnumerator TryCreateChickenMethods(DynamicThing chickenPrefab, Vector3 spawnPosition)
        {
            Debug.Log("Trying different creation methods...");
            
            // Метод 1: OnServer.Create<Chicken> (рекомендуемый)
            bool success = TryMethod1(chickenPrefab, spawnPosition);
            if (success) yield break;
            
            yield return null;
            
            // Метод 2: OnServer.Create<DynamicThing>
            success = TryMethod2(chickenPrefab, spawnPosition);
            if (success) yield break;
            
            yield return null;
            
            // Метод 3: OnServer.Create по имени префаба
            success = TryMethod3(spawnPosition);
            if (success) yield break;
            
            yield return null;
            
            // Метод 4: Последняя попытка с OnServer.CreateOld
            success = TryMethod4(chickenPrefab, spawnPosition);
            if (success) yield break;
            
            Debug.LogError("All chicken spawn methods failed!");
        }
        
        private static bool TryMethod1(DynamicThing chickenPrefab, Vector3 spawnPosition)
        {
            try
            {
                Debug.Log("Method 1: Trying OnServer.Create<Chicken>...");
                Chicken chickenObject = OnServer.Create<Chicken>(chickenPrefab, spawnPosition, Quaternion.identity);
                
                if (chickenObject != null)
                {
                    Debug.Log($"SUCCESS: Method 1 - Chicken spawned successfully at position: {spawnPosition}");
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Method 1 failed: {e.Message}");
            }
            return false;
        }
        
        private static bool TryMethod2(DynamicThing chickenPrefab, Vector3 spawnPosition)
        {
            try
            {
                Debug.Log("Method 2: Trying OnServer.Create<DynamicThing>...");
                DynamicThing chickenObject = OnServer.Create<DynamicThing>(chickenPrefab, spawnPosition, Quaternion.identity);
                
                if (chickenObject != null)
                {
                    Debug.Log($"SUCCESS: Method 2 - DynamicThing spawned successfully at position: {spawnPosition}");
                    Chicken chickenComponent = chickenObject.GetComponent<Chicken>();
                    if (chickenComponent != null)
                    {
                        Debug.Log("Chicken component found on spawned object");
                    }
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Method 2 failed: {e.Message}");
            }
            return false;
        }
        
        private static bool TryMethod3(Vector3 spawnPosition)
        {
            try
            {
                Debug.Log("Method 3: Trying OnServer.Create<Chicken> by name...");
                Chicken chickenObject = OnServer.Create<Chicken>("Chicken", spawnPosition, Quaternion.identity);
                
                if (chickenObject != null)
                {
                    Debug.Log($"SUCCESS: Method 3 - Chicken created by name at position: {spawnPosition}");
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Method 3 failed: {e.Message}");
            }
            return false;
        }
        
        private static bool TryMethod4(DynamicThing chickenPrefab, Vector3 spawnPosition)
        {
            try
            {
                Debug.Log("Method 4: Last resort - trying OnServer.CreateOld...");
                DynamicThing chickenObject = OnServer.CreateOld(chickenPrefab, spawnPosition, Quaternion.identity, 0uL);
                
                if (chickenObject != null)
                {
                    Debug.Log($"SUCCESS: Method 4 - OnServer.CreateOld worked at position: {spawnPosition}");
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Method 4 also failed: {e.Message}");
                Debug.LogError($"All spawn methods failed! Stack trace: {e.StackTrace}");
            }
            return false;
        }
        
        private static DynamicThing FindChickenPrefab()
        {
            try
            {
                // Ищем префаб Chicken
                
                // Поиск через Prefab.Find по имени
                var chickenPrefab = Prefab.Find("Chicken");
                if (chickenPrefab != null)
                {
                    Debug.Log("Found Chicken prefab through Prefab.Find");
                    return chickenPrefab as DynamicThing;
                }
                
                // Альтернативный поиск среди всех DynamicThing префабов
                foreach (var prefab in DynamicThing.DynamicThingPrefabs)
                {
                    if (prefab.name.Contains("Chicken"))
                    {
                        Debug.Log($"Found Chicken prefab in DynamicThingPrefabs: {prefab.name}");
                        return prefab;
                    }
                }
                
                Debug.LogWarning("Chicken prefab not found in any lookup method");
                return null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error finding Chicken prefab: {e.Message}");
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