using UnityEngine;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Networking;

namespace EndGameTargetColony
{
    public static class NPCSpawner
    {
        public static void SpawnNPC(Vector3 position)
        {
            try
            {
                // Ищем префаб курицы
                // В Stationeers курицы могут быть представлены как Animal или специальный тип
                GameObject chickenPrefab = FindNPCBasePrefab();
                
                if (chickenPrefab != null)
                {
                    // Создаем НПЦ (базовое существо) в мире
                    Vector3 spawnPosition = FindSafeSpawnPosition(position);
                    GameObject npc = Object.Instantiate(chickenPrefab, spawnPosition, Quaternion.identity);
                    
                    // Инициализируем НПЦ
                    InitializeNPC(npc);
                    
                    Debug.Log($"NPC Phase 1: Base creature spawned at position: {spawnPosition}");
                }
                else
                {
                    Debug.LogError("NPC base prefab not found!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error spawning NPC: {e.Message}");
            }
        }
        
        private static GameObject FindNPCBasePrefab()
        {
            // Попытка найти базовый префаб для НПЦ (курица как основа) через различные методы
            
            // Метод 1: Поиск через Resources
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Animals/Chicken");
            if (prefab != null) return prefab;
            
            // Метод 2: Поиск через существующих животных в мире
            Animal[] existingAnimals = Object.FindObjectsOfType<Animal>();
            foreach (var animal in existingAnimals)
            {
                if (animal.name.ToLower().Contains("chicken") || 
                    animal.GetType().Name.ToLower().Contains("chicken"))
                {
                    return animal.gameObject;
                }
            }
            
            // Метод 3: Создание базового животного (fallback)
            // Это может потребовать дополнительной настройки
            return CreateBasicAnimalPrefab();
        }
        
        private static GameObject CreateBasicAnimalPrefab()
        {
            // Создаем базовый объект животного
            GameObject animalObj = new GameObject("BasicNPC");
            
            // Добавляем базовый компонент Animal
            var animal = animalObj.AddComponent<Animal>();
            
            // Настраиваем животное
            // Здесь могут потребоваться дополнительные настройки
            // в зависимости от API Animal класса
            
            return animalObj;
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
        
        private static void InitializeNPC(GameObject npc)
        {
            // Инициализируем базовое существо для будущего НПЦ
            var animalComponent = npc.GetComponent<Animal>();
            if (animalComponent != null)
            {
                // Устанавливаем базовые параметры
                // Здесь могут быть дополнительные настройки
                // в зависимости от API Animal класса
                
                Debug.Log("NPC Animal component initialized successfully");
            }
            
            // Пока что не работаем с сетевой системой
            // В будущем можно добавить сетевую синхронизацию
        }
    }
}