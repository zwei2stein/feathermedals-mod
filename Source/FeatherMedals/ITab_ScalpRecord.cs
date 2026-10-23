using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

// A scalp, or the scalps hung on a belt or a scalp pole.
public class ITab_ScalpRecord : ITab
{
    private const float Padding = 10f;
    private const float ScrollbarWidth = 16f;

    private static readonly Color SeparatorColor = new(1f, 1f, 1f, 0.15f);

    private Vector2 scrollPosition;

    public ITab_ScalpRecord()
    {
        size = new Vector2(400f, 480f);
        labelKey = "TribalTrophies_Scalping_ScalpTab_Label";
    }

    public override bool IsVisible => Scalps(SelThing).Any();

    private static IEnumerable<Scalp> Scalps(Thing thing)
    {
        if (thing is Scalp scalp)
            yield return scalp;

        if (thing is Building_ScalpPole pole)
            foreach (var scalpItem in pole.StoredScalps())
            {
                yield return scalpItem;
            }

        var container = thing?.TryGetComp<CompTrophyContainer>();
        if (container != null)
            foreach (var held in container.Held)
            {
                if (held is Scalp heldScalp)
                    yield return heldScalp;
            }
    }

    protected override void FillTab()
    {
        var scalps = Scalps(SelThing);
        if (!scalps.Any())
            return;

        var outerRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(Padding);
        var viewWidth = outerRect.width - ScrollbarWidth;

        var totalHeight = 0f;
        foreach (var scalp in scalps)
            totalHeight += ScalpRecordDrawer.Height(scalp, viewWidth) + Padding;
        var viewRect = new Rect(0f, 0f, viewWidth, totalHeight);

        Widgets.BeginScrollView(outerRect, ref scrollPosition, viewRect);

        var curY = 0f;
        var i = 0;
        foreach (var scalp in scalps)
        {
            if (i > 0)
            {
                GUI.color = SeparatorColor;
                Widgets.DrawLineHorizontal(0f, curY - Padding / 2f, viewWidth);
                GUI.color = Color.white;
            }

            var rowHeight = ScalpRecordDrawer.Height(scalp, viewWidth);
            ScalpRecordDrawer.Draw(scalp, new Rect(0f, curY, viewWidth, rowHeight));
            curY += rowHeight + Padding;
            i++;
        }

        Widgets.EndScrollView();

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;
        GUI.color = Color.white;
    }
}