using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace VBQOL
{
    public class FallDamage
    {
        private static Collider LastGroundContactCollider;
        private static Vector3 LastGroundContactNormal;
        private static Vector3 LastGroundContactPoint;
        private static float LastMaxAirAltitude;
        private static bool GroundContact;

        [HarmonyPatch]
        internal class FallDamagePatch
        {
            private static HashSet<int> BlackListed { get; } = new HashSet<int>();

            static FallDamagePatch()
            {
                //try
                {
                    BlackListed = new HashSet<int>
                {
                    "Blob".GetStableHashCode(),
                    "BlobElite".GetStableHashCode(),
                    "BlobTar".GetStableHashCode()
                };
                }
                /* catch (Exception e)
                 {
                     Log.LogWarning("Failed to initialize list of creatures to not apply fall damage to.", e);
                 }*/
            }

            [HarmonyPatch(typeof(Character), "UpdateGroundContact")]
            [HarmonyPrefix]
            private static void RememberContact(Character __instance)
            {
                if (__instance.IsPlayer())
                {
                    return;
                }
                LastGroundContactCollider = __instance.m_lowestContactCollider;
                LastGroundContactNormal = __instance.m_groundContactNormal;
                LastGroundContactPoint = __instance.m_groundContactPoint;
                LastMaxAirAltitude = __instance.m_maxAirAltitude;
                GroundContact = __instance.m_groundContact;
            }

            [HarmonyPatch(typeof(Character), "UpdateGroundContact")]
            [HarmonyPostfix]
            private static void AddFallDamageToNonPlayers(Character __instance)
            {
                if (__instance.IsPlayer())
                {
                    return;
                }
                if (!GroundContact)
                {
                    return;
                }
                ZNetView nview = __instance.m_nview;
                int? num;
                if (nview == null)
                {
                    num = null;
                }
                else
                {
                    ZDO zdo = nview.GetZDO();
                    num = ((zdo != null) ? new int?(zdo.m_prefab) : null);
                }
                int? num2 = num;
                if (num2 != null && BlackListed.Contains(num2.Value))
                {
                    return;
                }
                float num3 = Mathf.Max(0f, LastMaxAirAltitude - __instance.transform.position.y);
                if (num3 > 4f)
                {
                    HitData hitData = new();
                    hitData.m_damage.m_damage = Mathf.Clamp01((num3 - 4f) / 16f) * 100f;
                    hitData.m_point = LastGroundContactPoint;
                    hitData.m_dir = LastGroundContactNormal;
                    __instance.Damage(hitData);
                }
            }
        }
    }
}