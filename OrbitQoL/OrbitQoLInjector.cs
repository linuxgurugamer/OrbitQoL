using System;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using KSP.UI.Screens.Mapview;
using System.Collections.Generic;

namespace OrbitQoLInjector
{

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class OrbitQoLInjector : MonoBehaviour
    {
        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new Harmony("OrbitQoLInjector");
            harmony.PatchAll(assembly);
        }
    }

    [HarmonyPatch(typeof(PatchRendering), "mnEnd_OnUpdateCaption", new Type[] { typeof(MapNode), typeof(MapNode.CaptionData) })]
    public static class mnEnd_OnUpdateCaptionOverride
    {
        private static void Postfix(PatchRendering __instance, ref MapNode n, ref MapNode.CaptionData cData)
        {
            switch (__instance.patch.patchEndTransition)
            {
                case Orbit.PatchTransitionType.ENCOUNTER:
                    double encounterUT = __instance.patch.UTsoi;
                    Orbit nextPatch = __instance.patch.nextPatch;
                    Vector3d encounterVel = nextPatch.getOrbitalVelocityAtUT(encounterUT);
                    cData.captionLine2 = "Rel-V: " + encounterVel.magnitude.ToString("0.##") + "m/s";
                    cData.captionLine3 = "Encounter Angle: " + Vector3d.Angle(nextPatch.referenceBody.orbit.getOrbitalVelocityAtUT(encounterUT), encounterVel).ToString("0.##") + "°";
                    break;
                case Orbit.PatchTransitionType.ESCAPE:
                    double escapeUT = __instance.patch.UTsoi;
                    Vector3d escapeVel = __instance.patch.getOrbitalVelocityAtUT(escapeUT);
                    cData.captionLine2 = "Exit-V: " + escapeVel.magnitude.ToString("0.##") + "m/s";
                    cData.captionLine3 = "Ejection Angle: " + Vector3d.Angle(__instance.patch.referenceBody.orbit.getOrbitalVelocityAtUT(escapeUT), escapeVel).ToString("0.##") + "°";
                    break;
                default:
                    break;
            }
        }
    }

    [HarmonyPatch(typeof(OrbitTargeter), "DropInvalidTargets")]
    public static class DropInvalidTargetsOverride
    {
        private static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(FlightGlobals), "UpdateInformation", new Type[] { typeof(bool) })]
    public static class UpdateInformationOverride
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo FlightGlobals_currentMainBody = AccessTools.Field(typeof(FlightGlobals), nameof(FlightGlobals.currentMainBody));
            MethodInfo FlightGlobals_SetVesselTarget = AccessTools.Method(typeof(FlightGlobals), nameof(FlightGlobals.SetVesselTarget));

            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            int numFlightGlobals_currentMainBodysSeen = 0;
            int numFlightGlobals_SetVesselTargetsSeen = 0;
            for (int i = 1; i < code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Br
                    && code[i - 1].opcode == OpCodes.Call
                    && ReferenceEquals(code[i - 1].operand, FlightGlobals_SetVesselTarget))
                {
                    numFlightGlobals_SetVesselTargetsSeen++;
                    if (numFlightGlobals_SetVesselTargetsSeen == 2)
                    {
                        for (int j = i; j >= 0; j--)
                        {
                            if (code[j].opcode == OpCodes.Ldsfld && ReferenceEquals(code[j].operand, FlightGlobals_currentMainBody))
                            {
                                numFlightGlobals_currentMainBodysSeen++;
                            }
                            OpCode opcode = code[j].opcode;
                            code[j].opcode = OpCodes.Nop;
                            code[j].operand = null;
                            if (numFlightGlobals_currentMainBodysSeen == 2 && opcode == OpCodes.Ldarg_0)
                            {
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            return code;
        }
    }
}
