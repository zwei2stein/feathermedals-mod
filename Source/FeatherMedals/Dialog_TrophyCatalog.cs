using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class Dialog_TrophyCatalog : Window
{
    private Vector2 _scrollPosition;
    private List<ThingDef> _medals;
    private HashSet<ThingDef> _locked;
    private Dictionary<ThingDef, List<ResearchProjectDef>> _missingResearch;
    
    private const float WINDOW_WIDTH = 500f;
    private const float WINDOW_HEIGHT = 600f;

    public Dialog_TrophyCatalog()
    {
        doCloseX = true;
        doCloseButton = false;
        absorbInputAroundWindow = false;
        forcePause = false;

        _medals = DefDatabase<ThingDef>.AllDefs
            .Where(d => d.thingClass != null && typeof(FeatherMedal).IsAssignableFrom(d.thingClass))
            .OrderBy(d => d.uiOrder)
            .ThenBy(d => d.label)
            .ToList();

        _locked = new HashSet<ThingDef>();
        _missingResearch = new Dictionary<ThingDef, List<ResearchProjectDef>>();
        foreach (var def in _medals)
        {
            var missing = GetMissingResearch(def);
            if (missing == null || missing.Count == 0) continue;
            _locked.Add(def);
            _missingResearch[def] = missing;
        }
    }

    private static List<ResearchProjectDef> GetMissingResearch(ThingDef def)
    {
        List<ResearchProjectDef> missing = null;
        foreach (var recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
        {
            if (recipe.ProducedThingDef != def) continue;
            if (recipe.researchPrerequisites != null)
                foreach (var r in recipe.researchPrerequisites)
                    if (!r.IsFinished)
                        (missing ??= new List<ResearchProjectDef>()).Add(r);
            return missing;
        }
        return null;
    }

    public override Vector2 InitialSize => new(WINDOW_WIDTH, WINDOW_HEIGHT);

    public override void DoWindowContents(Rect inRect)
    {
        // Header
        Text.Font = GameFont.Medium;
        var headerRect = new Rect(inRect.x, inRect.y, inRect.width, 35f);
        Widgets.Label(headerRect, "FeatherMedals_MedalCatalog".Translate());
        Text.Font = GameFont.Small;

        GUI.color = new Color(1f, 1f, 1f, 0.15f);
        Widgets.DrawLineHorizontal(inRect.x, headerRect.yMax + 2f, inRect.width);
        GUI.color = Color.white;

        // Scroll area
        var listRect = new Rect(inRect.x, headerRect.yMax + 10f, inRect.width, inRect.height - headerRect.height - 10f);
        var viewWidth = listRect.width - 16f;

        var totalHeight = _medals.Sum(d => UIHelper.GetRowHeight(d, viewWidth) + UIHelper.ROW_PADDING);
        var viewRect = new Rect(0f, 0f, viewWidth, totalHeight);

        Widgets.BeginScrollView(listRect, ref _scrollPosition, viewRect);

        var curY = 0f;
        foreach (var def in _medals)
        {
            _missingResearch.TryGetValue(def, out var missing);
            UIHelper.TrophyInfoWidget_Def(
                def,
                _locked.Contains(def),
                missing,
                viewWidth,
                ref curY);
        }

        Widgets.EndScrollView();
    }
    
}