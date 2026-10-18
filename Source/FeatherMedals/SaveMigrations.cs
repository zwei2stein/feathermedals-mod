using System;
using System.Collections.Generic;
using Verse;

namespace FeatherMedals;

// Once-per-save fixes for saves started before a mod feature existed.
public class SaveMigrations : GameComponent
{
    private const string HonoringWarriorsIssue = "HonoringWarriorsIssue";
    private const string ScalpTakingIssue = "ScalpTakingIssue";

    private HashSet<string> applied = new();

    public SaveMigrations(Game game)
    {
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        Apply(HonoringWarriorsIssue, MissingIssuePrecepts.AddCompatibleStanceToIdeosMissingHonoringWarriors);
        Apply(ScalpTakingIssue, MissingIssuePrecepts.AddCompatibleStanceToIdeosMissingScalpTaking);
    }

    // marked done only after it ran, a failed migration is retried on next load
    private void Apply(string name, Action migration)
    {
        if (applied.Contains(name))
            return;
        migration();
        applied.Add(name);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref applied, "applied", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            applied ??= new HashSet<string>();
    }
}