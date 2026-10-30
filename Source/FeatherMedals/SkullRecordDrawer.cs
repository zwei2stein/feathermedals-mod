using UnityEngine;
using Verse;

namespace TribalTrophies;

// One skull: vanilla only remembers whose it was.
public class SkullRecordDrawer : TrophyRowDrawer
{
    private readonly Thing skull;
    private readonly string victimName;

    public SkullRecordDrawer(Thing skull, string victimName)
    {
        this.skull = skull;
        this.victimName = victimName;
    }

    // Skull of (victim), or just the def label when nobody is remembered
    private string Title => victimName.NullOrEmpty()
        ? skull.def.LabelCap.ToString()
        : "TribalTrophies_Trophy_SkullTab_SkullOf".Translate(
            victimName.StripTags().Colorize(TrophyUIStyle.GoldColor).Named("VICTIM")).Resolve();

    protected override Thing IconThing => skull;

    protected override float IconSize => TrophyUIStyle.SkullIconSize;

    protected override bool IconStackOfOne => true;

    // title centered against the picture
    protected override float LayoutText(Rect area, bool draw)
    {
        var curY = area.y;
        UIHelper.LayoutLine(Title, GameFont.Small, Color.white, TextAnchor.MiddleCenter, area, ref curY, draw);
        return curY - area.y;
    }
}
