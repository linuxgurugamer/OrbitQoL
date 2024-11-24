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
        static ITargetable target;
        private static void Prefix(ref FlightGlobals __instance, ref bool fixedUpdate)
        {
            target = __instance.VesselTarget ?? null;
        }

        private static void Postfix(ref FlightGlobals __instance, ref bool fixedUpdate)
        {
            if (__instance.VesselTarget == null && target != null &&
                !(target.GetTransform() == null || (!target.GetActiveTargetable() && target.GetVessel() == __instance.activeVessel)))
            {
                __instance.SetVesselTarget(target);
            }
        }
    }
}
