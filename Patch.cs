
using HarmonyLib;
using UnityEngine;

namespace MyFirstPlugin
{
    internal class DSPPatch
    {
        [HarmonyPrefix, HarmonyPatch(typeof(UIStationWindow), "_OnOpen")]
        public static bool openStorageWindow(UIStationWindow __instance)
        {
            if (__instance.factory == null)
            {
                return true;
            }
            //如果有factory，跳过初始化过程
            __instance.transform.SetAsLastSibling();
            return false;
        }
    }

}