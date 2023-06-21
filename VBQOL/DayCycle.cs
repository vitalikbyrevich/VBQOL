using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace VBQOL
{
    public class DayCycle
    {
        [HarmonyPatch(typeof(EnvMan), "Awake")]
        static class EnvMan_Awake_Patch
        {

            public static void Postfix(ref long ___m_dayLengthSec)
            {
                if (!VBQOL.enableModDCConfig.Value)
                    return;
                VBQOL.vanillaDayLengthSec = ___m_dayLengthSec;
                ___m_dayLengthSec = (long)(Mathf.Round(VBQOL.vanillaDayLengthSec / 0.333f));
            }
        }
    }
}