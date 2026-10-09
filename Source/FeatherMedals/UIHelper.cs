using System.Text;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class UIHelper
{
    private static readonly Color GreenColor = new(0.4f, 0.9f, 0.4f);
    public static readonly Color GoldColor = new(0.9f, 0.85f, 0.4f);
    
    public static void DrawStatSummary(ThingDef def, Rect rect, ref float curY)
    {
        var statOffsets = def.equippedStatOffsets;
        if (statOffsets is { Count: > 0 })
        {
            Text.Font = GameFont.Tiny;
            GUI.color = GreenColor;
            Text.Anchor = TextAnchor.MiddleCenter;
            foreach (var stat in statOffsets)
            {
                var sign = stat.value > 0 ? "+" : "";
                var line = $"{stat.stat.LabelCap}: {sign}{stat.stat.ValueToString(stat.value)}";
                Widgets.Label(new Rect(rect.x, curY, rect.width, 24f), line);
                curY += 24f;
            }
            GUI.color = Color.white;
            curY += 8f;
            Text.Font = GameFont.Small;
        }
    }
    
    public static void DrawTraitChangeSummary(FeatherMedal featherMedal, float textX, Rect rect, ref float curY)
    {
        DrawTraitChangeSummary(featherMedal, new Rect(textX, rect.y, rect.width - textX, rect.height), ref curY, TextAnchor.MiddleLeft);
    }

    public static void DrawTraitChangeSummary(FeatherMedal featherMedal, Rect area, ref float curY, TextAnchor anchor)
    {
        DrawTraitChangeLine(featherMedal.GetDecoratedTraitLabel(true), GoldColor, area, ref curY, anchor);
        DrawTraitChangeLine(featherMedal.GetAddedTraitLabel(true), Color.green, area, ref curY, anchor);
        DrawTraitChangeLine(featherMedal.GetRemovedTraitLabel(true), Color.red, area, ref curY, anchor);
    }

    private static void DrawTraitChangeLine(string text, Color color, Rect area, ref float curY, TextAnchor anchor)
    {
        if (text == null) return;

        Text.Font = GameFont.Tiny;
        GUI.color = color;
        Text.Anchor = anchor;
        Widgets.Label(new Rect(area.x, curY, area.width, 24f), text);
        GUI.color = Color.white;
        curY += 24f;
        Text.Font = GameFont.Small;
    }

    public static string GetStatSummary(ThingDef def)
    {
        var offsets = def.equippedStatOffsets;
        if (offsets == null || offsets.Count == 0) return null;

        var sb = new StringBuilder();
        for (var i = 0; i < offsets.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            var val = offsets[i].value;
            sb.Append(val >= 0 ? "+" : "");
            sb.Append(offsets[i].stat.ValueToString(val));
            sb.Append(' ');
            sb.Append(offsets[i].stat.LabelCap);
        }
        return sb.ToString();
    }

    public static void DrawTraitsSummary(ThingDef medalDef, Rect rect, ref float curY)
    {
        var ext = medalDef.GetModExtension<TrophyExtension>();
        
        Text.Font = GameFont.Tiny;
        if (ext != null && ((ext.removesTraits is { Count: > 0 }) || (ext.addsTraits is { Count: > 0 })))
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            if (ext.removesTraits != null)
            {
                foreach (var entry in ext.removesTraits)
                {
                    var label = entry.Label;
                    GUI.color = Color.grey;
                    var line = "FeatherMedals_AwardingMayRemove".Translate(label.CapitalizeFirst(), entry.chance.ToStringPercent());
                    Widgets.Label(new Rect(rect.x, curY, rect.width, 24f), line);
                    curY += 24f;
                }
            }
            if (ext.addsTraits != null)
            {
                foreach (var entry in ext.addsTraits)
                {
                    var label = entry.Label;
                    GUI.color = Color.grey;;
                    var line = "FeatherMedals_AwardingMayGrant".Translate(label.CapitalizeFirst(), entry.chance.ToStringPercent());
                    Widgets.Label(new Rect(rect.x, curY, rect.width, 24f), line);
                    curY += 24f;
                }
            }
            
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
    }

    public static void DrawCitation(ThingDef medalDef, string citation, Rect rect, ref float curY)
    {
        GUI.color = GoldColor;
        Text.Anchor = TextAnchor.MiddleCenter;
        var citationText = $"\"{citation}\"";
        var citationHeight = Text.CalcHeight(citationText, rect.width - 40f);
        var citationRect = new Rect(rect.x + 20f, curY, rect.width - 40f, citationHeight);
        Widgets.Label(citationRect, citationText);
        GUI.color = Color.white;
        curY += citationHeight + 12f;
    }
}