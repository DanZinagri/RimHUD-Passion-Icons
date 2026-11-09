using System;
using HarmonyLib;
using Verse;
using VSE;

namespace RimHUDPassionIcons.VanillaSkills
{
    // VSE only runs its RimHUD patch (passion indicators and colors on the skill rows) when a mod
    // named exactly "RimHUD" is active, so it skips RimHUD FIX. Its queued patch reads
    // ModCompat.RimHUD when it runs, so setting that flag right after Init is enough.
    internal static class RimHUDFixCompat
    {
        private static bool initPostfixRan;

        public static void Install(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(ModCompat), nameof(ModCompat.Init)),
                postfix: new HarmonyMethod(typeof(RimHUDFixCompat), nameof(Init_Postfix)));
        }

        private static void Init_Postfix()
        {
            initPostfixRan = true;
            if (RimHUDPassionIconsMod.RimHUDActive)
                ModCompat.RimHUD = true;
        }

        // VSE's Mod constructor ran before ours (load order ignored loadBefore) and Init finished,
        // so its queued RimHUD patch has already run and skipped. (If Init threw instead,
        // PrepareModeratelyFix.Fallback has just applied the patch and set the flag.)
        public static void Late()
        {
            if (initPostfixRan || ModCompat.RimHUD || !RimHUDPassionIconsMod.RimHUDActive)
                return;
            ModCompat.RimHUD = true;
            try { RimHUDPatches.Do(SkillsMod.Harm); }
            catch (Exception e) { Log.Warning("[RimHUD Passion Icons - VSE] VSE's RimHUD patch failed: " + e); }
        }
    }
}
