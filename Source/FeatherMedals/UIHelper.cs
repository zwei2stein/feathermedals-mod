using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class UIHelper
{
    private const float ICON_SIZE = 64f;

    public const float ROW_PADDING = 10f;
    private const float MIN_ROW_HEIGHT = 80f;
    
    private static readonly Color GreenColor = new(0.4f, 0.9f, 0.4f);
    public static readonly Color GoldColor = new(0.9f, 0.85f, 0.4f);
    
    // Cached across dialog openings — defs and their textures are stable for the session
    private static readonly Dictionary<Texture, Texture2D> _grayscaleCache = new();

    
    public static void TrophyInfoWidget_Def(ThingDef def, bool locked, List<ResearchProjectDef> missing, float width,
        ref float curY)
    {
        var rowHeight = GetRowHeight(def, width);
        var rowRect = new Rect(0f, curY, width, rowHeight);

        if (Mouse.IsOver(rowRect))
            Widgets.DrawHighlight(rowRect);

        // Icon
        var iconRect = new Rect(ROW_PADDING, curY + (rowHeight - ICON_SIZE) / 2f, ICON_SIZE, ICON_SIZE);
        if (locked)
        {
            var gray = GetGrayscale(def.uiIcon);
            if (gray != null)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                GUI.DrawTexture(iconRect, gray);
                GUI.color = Color.white;
            }
            else
            {
                Widgets.DefIcon(iconRect, def);
            }

            var lockSize = ICON_SIZE * 0.4f;
            GUI.color = GoldColor;
            GUI.DrawTexture(
                new Rect(iconRect.xMax - lockSize, iconRect.yMax - lockSize, lockSize, lockSize),
                TrophyTextures.LockedIcon);
            GUI.color = Color.white;

            if (missing.Count > 0)
            {
                var sb = new StringBuilder();
                for (var i = 0; i < missing.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(missing[i].LabelCap);
                }
                TooltipHandler.TipRegion(iconRect, "FeatherMedals_MedalLockedResearch".Translate(sb.ToString()));
            }
        }
        else
        {
            Widgets.DefIcon(iconRect, def);
        }

        var textX = iconRect.xMax + ROW_PADDING;
        var textWidth = width - ICON_SIZE - (ROW_PADDING * 3);
        var textY = curY + ROW_PADDING;

        // Name
        var nameValue = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(def.LabelCap);
        Text.Font = GameFont.Small;
        var nameHeight = Text.CalcHeight(nameValue, textWidth);
        Widgets.Label(new Rect(textX, textY, textWidth, nameHeight), nameValue);
        textY += nameHeight + 2f;

        // Description
        Text.Font = GameFont.Tiny;
        GUI.color = Color.gray;
        var desc = def.description ?? "";
        var descHeight = Text.CalcHeight(desc, textWidth);
        Widgets.Label(new Rect(textX, textY, textWidth, descHeight), desc);
        GUI.color = Color.white;
        textY += descHeight + 2f;

        // Honor
        var ext = def.GetModExtension<ThrophyExtension>();
        var honor = ext?.honorAwarded ?? 0;
        if (ModsConfig.RoyaltyActive && honor > 0)
        {
            GUI.color = GoldColor;
            var iconSize = 14f;
            var gap = 4f;
            var labelText = "FeatherMedals_HonorLabel".Translate(honor.ToString());
            if (TrophyTextures.HonorIcon != null)
                GUI.DrawTexture(new Rect(textX, textY + 2f, iconSize, iconSize), TrophyTextures.HonorIcon);
            Widgets.Label(new Rect(textX + iconSize + gap, textY, textWidth - iconSize - gap, 18f), labelText);
            GUI.color = Color.white;
            textY += 18f + 2f;
        }

        // Stat offsets
        var statText = GetStatSummary(def);
        if (!statText.NullOrEmpty())
        {
            Text.Font = GameFont.Tiny;
            GUI.color = GreenColor;
            var statHeight = Text.CalcHeight(statText, textWidth);
            Widgets.Label(new Rect(textX, textY, textWidth, statHeight), statText);
            GUI.color = Color.white;
        }

        Text.Font = GameFont.Small;

        // Separator
        GUI.color = new Color(1f, 1f, 1f, 0.08f);
        Widgets.DrawLineHorizontal(0f, curY + rowHeight, width);
        GUI.color = Color.white;

        curY += rowHeight + ROW_PADDING;
    }
    
    public static void TrophyInfoWidget_Adorned()
    {
        
    }

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
    
    public static void DrawTraitChangeSummary(FeatherMedal featherMedal, Rect rect, ref float curY)
    {

        if (featherMedal.addedTrait != null)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = GreenColor;
            Text.Anchor = TextAnchor.MiddleCenter;
            
            Widgets.Label(new Rect(rect.x, curY, rect.width, 24f), "FeatherMedals_Summary_TraitAdded".Translate(
                featherMedal.addedTrait.DataAtDegree(featherMedal.addedTraitDegree).LabelCap.Named("TRAIT")
                ));
            
            GUI.color = Color.white;
            curY += 8f;
            Text.Font = GameFont.Small;
        }
        
        if (featherMedal.removedTrait != null)
        {
            Text.Font = GameFont.Tiny;
            GUI.color = GreenColor;
            Text.Anchor = TextAnchor.MiddleCenter;
            
            Widgets.Label(new Rect(rect.x, curY, rect.width, 24f), "FeatherMedals_Summary_TraitAdded".Translate(
                featherMedal.removedTrait.DataAtDegree(featherMedal.removedTraitDegree).LabelCap.Named("TRAIT")
            ));
            
            GUI.color = Color.white;
            curY += 8f;
            Text.Font = GameFont.Small;
        }
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

    public static float GetRowHeight(ThingDef def, float width)
    {
        var textWidth = width - ICON_SIZE - (ROW_PADDING * 3);
        var height = ROW_PADDING;

        Text.Font = GameFont.Small;
        height += Text.CalcHeight(def.LabelCap, textWidth) + 2f;

        Text.Font = GameFont.Tiny;
        height += Text.CalcHeight(def.description ?? "", textWidth) + 2f;

        var statText = GetStatSummary(def);
        if (!statText.NullOrEmpty())
            height += Text.CalcHeight(statText, textWidth) + 2f;

        var ext = def.GetModExtension<ThrophyExtension>();
        if (ModsConfig.RoyaltyActive && (ext?.honorAwarded ?? 0) > 0)
            height += 18f + 2f;

        height += ROW_PADDING;
        Text.Font = GameFont.Small;
        return Mathf.Max(MIN_ROW_HEIGHT, height);
    }
    
    
    private static Texture2D GetGrayscale(Texture src)
    {
        if (src == null) return null;
        if (_grayscaleCache.TryGetValue(src, out var cached) && cached != null) return cached;

        var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;

        var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        var px = tex.GetPixels();
        for (var i = 0; i < px.Length; i++)
        {
            var c = px[i];
            var l = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            px[i] = new Color(l, l, l, c.a);
        }
        tex.SetPixels(px);
        tex.Apply();

        _grayscaleCache[src] = tex;
        return tex;
    }

    public static void DrawTraitsSummary(ThingDef medalDef, Rect rect, ref float curY)
    {
        var ext = medalDef.GetModExtension<ThrophyExtension>();
        
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

    public static void DrawHonorSummary(ThingDef medalDef, Rect rect, ref float curY)
    {
        var ext = medalDef.GetModExtension<ThrophyExtension>();
        var honor = ext?.honorAwarded ?? 0;
        
        Text.Font = GameFont.Tiny;
        GUI.color = GoldColor;
        Text.Anchor = TextAnchor.MiddleCenter;
        var iconSize = 14f;
        
        var totalWidth = iconSize + 4f + Text.CalcSize("FeatherMedals_HonorLabel".Translate(honor.ToString())).x;
        var startX = rect.x + (rect.width - totalWidth) / 2f;
        var labelRect = new Rect(startX + iconSize + 4f, curY, totalWidth - iconSize - 4f, 24f);
        
        if (TrophyTextures.HonorIcon != null)
            GUI.DrawTexture(new Rect(startX, curY + 5f, iconSize, iconSize), TrophyTextures.HonorIcon);
        Widgets.Label(labelRect, "FeatherMedals_HonorLabel".Translate(honor.ToString()));
        curY += 8f + 24f;
        GUI.color = Color.white;
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        
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