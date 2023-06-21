using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace VBQOL
{
    public class BetterLaders
    {
        [HarmonyPatch(typeof(AutoJumpLedge), "OnTriggerStay")]
        public static class Ladder_Patch
        {
            private static bool Prefix(AutoJumpLedge __instance, Collider collider)
            {
                Character component = collider.GetComponent<Character>();
                bool flag = component /*&& !DEBUG*/ && component == Player.m_localPlayer;
                if (flag)
                {
                    //  DEBUG = true;
                    Vector3 position = component.transform.position;
                    float y = __instance.gameObject.transform.rotation.eulerAngles.y;
                    float y2 = component.transform.rotation.eulerAngles.y;
                    float num = Math.Abs(Mathf.DeltaAngle(y, y2));
                    bool flag2 = num <= 12f;
                    if (flag2)
                    {
                        bool flag3 = !component.m_running;
                        if (flag3)
                        {
                            component.transform.position = new Vector3(position.x, position.y + 0.06f, position.z) + component.transform.forward * 0.08f;
                        }
                        else
                        {
                            component.transform.position = new Vector3(position.x, position.y + 0.08f, position.z) + component.transform.forward * 0.08f;
                        }
                    }
                }
                return !(component == Player.m_localPlayer);
            }
        }
    }
}