using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VSE.Passions;

namespace RimHUDPassionIcons.VanillaSkills
{
    // Loaded only when Vanilla Skills Expanded is active (LoadFolders IfModActive), so nothing in
    // the main assembly ever touches VSE types.
    public class VSEIntegrationMod : Mod
    {
        public VSEIntegrationMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("DanZinagri.RimHUDPassionIcons.VSE");

            // Must be in place before VSE's own Mod constructor calls ModCompat.Init
            // (About.xml loads us before VSE). Fallback() covers a load order where it isn't.
            PrepareModeratelyFix.Install(harmony);
            LongEventHandler.ExecuteWhenFinished(() => PrepareModeratelyFix.Fallback(harmony));

            PassionIconPatches.Resolver = PassionDefIcon;
        }

        // Every VSE passion (and Alpha Skills' ones) is a PassionDef with its own icon.
        private static Texture2D PassionDefIcon(SkillRecord skill, out string indicator)
        {
            indicator = null;
            PassionDef passion = PassionManager.PassionToDef(skill.passion);
            if (passion == null || passion.iconPath.NullOrEmpty())
                return null;
            indicator = passion.Indicator;
            return passion.Icon;
        }
    }
}
