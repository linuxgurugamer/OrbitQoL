using System;
using UnityEngine;
using System.Reflection;
using HarmonyLib;
using KSP.UI.Screens.Mapview;

namespace OrbitQoLInjector
{

    [KSPAddon(KSPAddon.Startup.Flight, true)]
    public class OrbitQoLInjector : MonoBehaviour
    {
        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new Harmony("CameraInjector");
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
                    Orbit nextPatch = __instance.patch.nextPatch;
                    cData.captionLine2 = "Rel-V: " + nextPatch.getOrbitalSpeedAt(nextPatch.StartUT).ToString("0.##") + "m/s";
                    break;
                case Orbit.PatchTransitionType.ESCAPE:
                    cData.captionLine2 = "Exit-V: " + __instance.patch.getOrbitalSpeedAt(__instance.patch.EndUT).ToString("0.##") + "m/s";
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
        static ITargetable target;
        private static void Prefix(ref FlightGlobals __instance, ref bool fixedUpdate)
        {
            target = __instance.VesselTarget ?? null;
        }
        private static void Postfix(ref FlightGlobals __instance, ref bool fixedUpdate)
        {
            if (__instance.VesselTarget == null && target != null)
            {
                __instance.SetVesselTarget(target);
                if (__instance.VesselTarget != null)
                {
                    if (__instance.VesselTarget.GetTransform() == null)
                    {
                        __instance.SetVesselTarget(null);
                    }
                    else if (!__instance.VesselTarget.GetActiveTargetable())
                    {
                        if (__instance.VesselTarget.GetVessel() == __instance.activeVessel)
                        {
                            __instance.SetVesselTarget(null);
                        }
                    }
                }
            }
        }
    }
}
