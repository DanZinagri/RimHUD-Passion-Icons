using System.Runtime.CompilerServices;
using HarmonyLib;
using RimHUD.Configuration;
using RimHUD.Configuration.Settings;
using RimHUD.Extensions;
using RimHUD.Interface;
using RimHUD.Interface.Hud.Models;
using RimHUD.Interface.Hud.Models.Values;
using RimHUD.Interface.Hud.Widgets;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimHUDPassionIcons
{
    // Returns the icon for a skill's passion (null = draw nothing) and the text indicator RimHUD
    // put after the skill name for it (null if it's only RimHUD's own stars).
    public delegate Texture2D PassionIconResolver(SkillRecord skill, out string indicator);

    // RimHUD draws passions as text after the skill name: its own stars, or VSE's indicatorString
    // once VSE's RimHUD patch runs. IMGUI labels can't hold images, so when a skill widget is built
    // we cut that indicator off the label and remember the icon, then draw it just after the label
    // text. Vanilla icons by default; the VSE integration assembly swaps in PassionDef icons.
    public static class PassionIconPatches
    {
        private const string ColorClose = "</color>";

        public static PassionIconResolver Resolver = VanillaIcon;

        private static readonly ConditionalWeakTable<ValueWidget, Texture2D> Icons = new ConditionalWeakTable<ValueWidget, Texture2D>();

        private static readonly AccessTools.FieldRef<SkillValue, SkillRecord> SkillRef =
            AccessTools.FieldRefAccess<SkillValue, SkillRecord>("_skill");
        private static readonly AccessTools.FieldRef<StandardWidget, string> LabelRef =
            AccessTools.FieldRefAccess<StandardWidget, string>("<Label>k__BackingField");
        private static readonly AccessTools.FieldRef<StandardWidget, TextStyle> StyleRef =
            AccessTools.FieldRefAccess<StandardWidget, TextStyle>("_textStyle");

        internal static void Patch(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(ValueModel), nameof(ValueModel.Build)),
                postfix: new HarmonyMethod(typeof(PassionIconPatches), nameof(Build_Postfix)));
            harmony.Patch(AccessTools.Method(typeof(ValueWidget), nameof(ValueWidget.Draw)),
                postfix: new HarmonyMethod(typeof(PassionIconPatches), nameof(Draw_Postfix)));
        }

        private static void Build_Postfix(ValueModel __instance, IWidget __result)
        {
            if (!(__instance is SkillValue model) || !(__result is ValueWidget widget))
                return;
            SkillRecord skill = SkillRef(model);
            if (skill == null || skill.passion == Passion.None)
                return;
            Texture2D icon = Resolver(skill, out string indicator);
            if (icon == null)
                return;

            string label = LabelRef(widget);
            string stripped = StripIndicator(label, indicator)
                ?? StripIndicator(label, StarIndicator((int)skill.passion));
            if (stripped == null)
                return;

            LabelRef(widget) = stripped;
            Icons.Remove(widget);
            Icons.Add(widget, icon);
        }

        private static Texture2D VanillaIcon(SkillRecord skill, out string indicator)
        {
            indicator = null;
            switch (skill.passion)
            {
                case Passion.Minor: return SkillUI.PassionMinorIcon;
                case Passion.Major: return SkillUI.PassionMajorIcon;
                default: return null;
            }
        }

        private static void Draw_Postfix(ValueWidget __instance, Rect rect)
        {
            if (!Icons.TryGetValue(__instance, out Texture2D icon))
                return;
            string label = LabelRef(__instance);
            if (label == null)
                return;

            // Same split as ValueWidget.Draw: [0] whole row, [1] label column, [2] value.
            Rect labelRect = rect.GetHGrid(2f, Theme.LabelWidth.Value, -1f)[1];
            GUIStyle style = StyleRef(__instance).GUIStyle;
            float size = Mathf.Min(labelRect.height, style.lineHeight);
            float x = Mathf.Min(labelRect.x + GUIPlus.GetTextSize(style, label).x, labelRect.xMax - size);
            Rect iconRect = new Rect(x, labelRect.y + (labelRect.height - size) / 2f, size, size);

            Color color = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            GUI.color = color;
        }

        // RimHUD's own indicator: its star string repeated once per passion value.
        private static string StarIndicator(int count)
        {
            string star = "RimHUD.Model.Skill.PassionIndicator".Translate();
            return count <= 0 || star.NullOrEmpty() ? null : new System.Text.StringBuilder().Insert(0, star, count).ToString();
        }

        // Label is ColoredText.Colorize(name + indicator), so the indicator sits just before </color>.
        private static string StripIndicator(string label, string indicator)
        {
            if (label == null || indicator.NullOrEmpty())
                return null;
            if (label.EndsWith(indicator + ColorClose))
                return label.Substring(0, label.Length - indicator.Length - ColorClose.Length) + ColorClose;
            if (label.EndsWith(indicator))
                return label.Substring(0, label.Length - indicator.Length);
            return null;
        }
    }
}
