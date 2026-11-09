using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;

namespace RimHUDPassionIcons
{
    public class RimHUDPassionIconsMod : Mod
    {
        public RimHUDPassionIconsMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("DanZinagri.RimHUDPassionIcons");
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (RimHUDActive)
                    PatchRimHUD(harmony);
            });
        }

        // Jaxe's RimHUD, or RimHUD FIX: a byte-identical RimHUD.dll relisted for 1.7 under its own
        // packageId and name, so name checks for "RimHUD" (ours and VSE's) miss it.
        public static bool RimHUDActive =>
            ModLister.GetActiveModWithIdentifier("Jaxe.RimHUD", ignorePostfix: true) != null
            || ModLister.GetActiveModWithIdentifier("RuoXue.RimHUDFix", ignorePostfix: true) != null;

        // Kept out of line so RimHUD.dll is only resolved when RimHUD is actually loaded.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void PatchRimHUD(Harmony harmony) => PassionIconPatches.Patch(harmony);
    }
}
