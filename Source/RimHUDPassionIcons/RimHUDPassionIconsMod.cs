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

            // Must be in place before VSE's own Mod constructor calls ModCompat.Init
            // (About.xml loads us before VSE). Fallback() covers a load order where it isn't.
            PrepareModeratelyFix.Install(harmony);

            LongEventHandler.ExecuteWhenFinished(() =>
            {
                PrepareModeratelyFix.Fallback(harmony);
                if (ModLister.HasActiveModWithName("RimHUD"))
                    PatchRimHUD(harmony);
            });
        }

        // Kept out of line so RimHUD.dll is only resolved when RimHUD is actually loaded.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void PatchRimHUD(Harmony harmony) => PassionIconPatches.Patch(harmony);
    }
}
