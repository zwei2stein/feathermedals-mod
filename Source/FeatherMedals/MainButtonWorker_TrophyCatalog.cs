using RimWorld;
using Verse;

namespace FeatherMedals;

public class MainButtonWorker_TrophyCatalog : MainButtonWorker
{
    public override bool Visible => MedalMod.Settings.ShowTrophyCatalog;

    public override void Activate()
    {
        Find.WindowStack.Add(new Dialog_TrophyCatalog());
    }
}