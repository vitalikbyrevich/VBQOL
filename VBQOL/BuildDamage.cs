using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace VBQOL
{
    public class BuildDamage
    {
        [HarmonyPatch(typeof(WearNTear), "RPC_HealthChanged")]
        public static class RPC_HealthChanged_Patch
        {
            public static bool Prefix(long peer, Piece ___m_piece)
            {
                if (___m_piece is null)
                    return true;
                //Dbgl($"creator: {___m_piece.GetCreator()} peer {peer}");

                if (VBQOL.uncreatedDamageMultConfig.Value == 0 && ___m_piece.GetCreator() == 0)
                    return false;

                if (VBQOL.nonCreatorDamageMultConfig.Value == 0 &&
                    (___m_piece.GetCreator() != 0 && peer != ___m_piece.GetCreator()))
                    return false;

                if (VBQOL.creatorDamageMultConfig.Value == 0 &&
                    (___m_piece.GetCreator() != 0 && peer == ___m_piece.GetCreator()))
                    return false;
                return true;
            }
        }

        [HarmonyPatch(typeof(WearNTear), "RPC_Damage")]
        public static class RPC_Damage_Patch
        {
            public static void Prefix(ref HitData hit, Piece ___m_piece)
            {
                if (!VBQOL.enableModBDConfig.Value) return;
                if (!___m_piece) return;
                float mult;
                var zdo = ___m_piece.m_nview.GetZDO();
                string creatorName = zdo.GetString("creatorName");
                var attacker = hit.GetAttacker();
                var player = (attacker as Player);
                if (attacker != null && attacker.IsPlayer())
                {
                    if (___m_piece?.GetCreator() == 0)
                    {
                        _ = VBQOL.uncreatedDamageMultConfig.Value;
                    }

                    if (creatorName == player.GetPlayerName())
                    {
                        mult = VBQOL.creatorDamageMultConfig.Value;
                    }
                    else
                    {
                        mult = VBQOL.nonCreatorDamageMultConfig.Value;
                    }
                }
                else
                {
                    mult = VBQOL.naturalDamageMultConfig.Value;
                }

                MultiplyDamage(ref hit, mult);
            }

            private static void MultiplyDamage(ref HitData hit, float value)
            {
                value = Math.Max(0, value);
                hit.m_damage.m_damage *= value;
                hit.m_damage.m_blunt *= value;
                hit.m_damage.m_slash *= value;
                hit.m_damage.m_pierce *= value;
                hit.m_damage.m_chop *= value;
                hit.m_damage.m_pickaxe *= value;
                hit.m_damage.m_fire *= value;
                hit.m_damage.m_frost *= value;
                hit.m_damage.m_lightning *= value;
                hit.m_damage.m_poison *= value;
                hit.m_damage.m_spirit *= value;
            }
        }

        [HarmonyPatch(typeof(WearNTear), "ApplyDamage")]
        public static class ApplyDamage_Patch
        {
            public static void Prefix(ref float damage)
            {
                if (!VBQOL.enableModBDConfig.Value || Environment.StackTrace.Contains("RPC_Damage"))
                    return;
                damage *= VBQOL.naturalDamageMultConfig.Value;
            }
        }
    }
}