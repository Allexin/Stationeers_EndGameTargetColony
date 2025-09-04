using HarmonyLib;
using Assets.Scripts.Objects.Entities;
using UnityEngine;

namespace EndGameTargetColony
{
    [HarmonyPatch(typeof(Chicken))]
    public class ChickenPatch
    {
        [HarmonyPatch("OnAtmosphericTick")]
        [HarmonyPrefix]
        public static bool OnAtmosphericTick_Prefix(Chicken __instance)
        {
            return true;
        }
    }
}
