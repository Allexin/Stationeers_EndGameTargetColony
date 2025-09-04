using HarmonyLib;
using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace EndGameTargetColony
{
    [HarmonyPatch(typeof(FertilizedEgg))]
    public class FertilizedEggPatch
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void Awake_Postfix(FertilizedEgg __instance)
        {
            // Устанавливаем время вылупления на 5 секунд для яиц созданных нашим модом
            // Проверяем что это наше яйцо (можно по позиции или другим способом)
            __instance.HatchTime = 5f;
            Debug.Log($"FertilizedEgg patch: Set hatch time to 5 seconds for egg at {__instance.transform.position}");
        }
    }
}