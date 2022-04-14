
using HarmonyLib;
using UnityEngine;

namespace MyFirstPlugin
{
    internal class DSPPatch
    {
        [HarmonyPrefix, HarmonyPatch(typeof(UIStationWindow), "_OnOpen")]
        public static bool openStorageWindow(UIStationWindow __instance)
        {
            Debug.Log(__instance.stationId);
            //如果有factory，跳过初始化过程
            if (__instance.factory == null)
            {
                return true;
            }
            Debug.Log(__instance.factory.planet);
            __instance.transform.SetAsLastSibling();
            return false;
        }
    }

}