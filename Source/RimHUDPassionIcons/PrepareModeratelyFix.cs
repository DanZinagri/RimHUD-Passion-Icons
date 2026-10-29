using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using VSE;
using VSE.Passions;

namespace RimHUDPassionIcons
{
    // VSE's PrepareModeratelyPatches.Do patches Lakuna.PrepareModerately.Filter.Part.PawnFilterPart
    // .GetRandomOfEnum, which Prepare Moderately removed long ago. AccessTools returns null, Harmony
    // throws, and the exception unwinds out of VSE's ModCompat.Init before it reaches the Stranger
    // Danger hook and the queued RimHUD patch, so RimHUD never learns about VSE passions.
    //
    // We replace Do with the same patches applied one by one, skipping any target that no longer
    // exists. GetRandomOfEnum's job (random VSE passion when a filter part is randomized) moved to
    // Randomize postfixes on PM's two passion filter parts.
    internal static class PrepareModeratelyFix
    {
        private const string PartTypes = "Lakuna.PrepareModerately.Filter.Part.Types.";

        private static Harmony ours;
        private static bool handled;

        public static void Install(Harmony harmony)
        {
            ours = harmony;
            harmony.Patch(AccessTools.Method(typeof(PrepareModeratelyPatches), nameof(PrepareModeratelyPatches.Do)),
                prefix: new HarmonyMethod(typeof(PrepareModeratelyFix), nameof(Do_Prefix)));
        }

        private static bool Do_Prefix(Harmony harm)
        {
            handled = true;
            Apply(harm);
            return false;
        }

        // Only matters if VSE's Mod constructor ran before ours (load order ignored loadBefore):
        // its Init already threw, so apply the PM patches and finish the two steps it skipped.
        public static void Fallback(Harmony harmony)
        {
            if (handled || !ModCompat.PrepareModerately)
                return;
            handled = true;
            ours = harmony;
            Apply(SkillsMod.Harm);

            if (ModCompat.StrangerDanger && ModCompat.SD_passion == null)
                ModCompat.SD_passion = AccessTools.TypeByName("Stranger_Danger.BioSkillsPatch")?.GetMethod("passion_sd");

            if (ModCompat.RimHUD)
            {
                try { RimHUDPatches.Do(SkillsMod.Harm); }
                catch (Exception e) { DevLog("VSE's RimHUD patch failed: " + e); }
            }
        }

        private static void Apply(Harmony vse)
        {
            TryPatch(vse, AccessTools.Method(typeof(FloatMenuUtility), nameof(FloatMenuUtility.MakeMenu), null, new[] { typeof(Passion) }),
                prefix: AccessTools.Method(typeof(PrepareModeratelyPatches), nameof(PrepareModeratelyPatches.MakeMenu_Prefix)));

            MethodInfo passionString = AccessTools.Method(typeof(PrepareModeratelyPatches), nameof(PrepareModeratelyPatches.PassionStringTranspiler));
            MethodInfo randomize = AccessTools.Method(typeof(PrepareModeratelyFix), nameof(Randomize_Postfix));
            foreach (string name in new[] { "HasPassion", "HasPassionsAtLevel" })
            {
                Type type = AccessTools.TypeByName(PartTypes + name);
                if (type == null)
                {
                    DevLog("Prepare Moderately type " + name + " not found, skipped.");
                    continue;
                }
                TryPatch(vse, AccessTools.Method(type, "DoEditInterface"), transpiler: passionString);
                TryPatch(vse, AccessTools.Method(type, "Summary"), transpiler: passionString);
                TryPatch(ours, AccessTools.Method(type, "Randomize"), postfix: randomize);
            }
        }

        private static void Randomize_Postfix(ref Passion ____passion)
        {
            ____passion = PassionManager.AllPassions.RandomElement();
        }

        private static void TryPatch(Harmony harmony, MethodBase target, MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo transpiler = null)
        {
            if (target == null)
            {
                DevLog("patch target missing, skipped (" + (prefix ?? postfix ?? transpiler).Name + ").");
                return;
            }
            Patches info = Harmony.GetPatchInfo(target);
            if (info != null && info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers)
                    .Any(p => p.PatchMethod == prefix || p.PatchMethod == postfix || p.PatchMethod == transpiler))
                return;
            try
            {
                harmony.Patch(target,
                    prefix: prefix == null ? null : new HarmonyMethod(prefix),
                    postfix: postfix == null ? null : new HarmonyMethod(postfix),
                    transpiler: transpiler == null ? null : new HarmonyMethod(transpiler));
            }
            catch (Exception e)
            {
                DevLog("patching " + target.DeclaringType?.Name + "." + target.Name + " failed: " + e.Message);
            }
        }

        private static void DevLog(string text)
        {
            if (Prefs.DevMode)
                Log.Message("[RimHUD Passion Icons] " + text);
        }
    }
}
