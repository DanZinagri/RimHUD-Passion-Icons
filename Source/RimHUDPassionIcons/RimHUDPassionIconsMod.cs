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
                if (ModLister.HasActiveModWithName("RimHUD"))
                    PatchRimHUD(harmony);
            });
        }

        // Kept out of line so RimHUD.dll is only resolved when RimHUD is actually loaded.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void PatchRimHUD(Harmony harmony) => PassionIconPatches.Patch(harmony);
    }
}
