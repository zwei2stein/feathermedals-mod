using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FeatherMedals
{
    public class FeatherMedal : Apparel
    {
        public CompBiocodable BiocodeComp => field ??= this.GetComp<CompBiocodable>();
        private Pawn _cachedPawn = null;
            
        public string citation;
        public int ceremonyQuality = -1; // -1 = not yet awarded
        public bool isLocked = true;
        public Pawn awardedBy;
        public int awardedTick = -1;

        public TraitDef addedTrait = null;
        public int addedTraitDegree = 0;
        public TraitDef removedTrait = null;
        public int removedTraitDegree = 0;
            
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref citation, "citation");
            Scribe_Values.Look(ref ceremonyQuality, "ceremonyQuality", -1);
            Scribe_Values.Look(ref isLocked, "isLocked", true);
            Scribe_References.Look(ref awardedBy, "awardedBy", true);
            Scribe_Values.Look(ref awardedTick, "awardedTick", -1);
            
            Scribe_Defs.Look(ref addedTrait, "addedTrait");
            Scribe_Values.Look(ref addedTraitDegree, "addedTraitDegree", 0);
            Scribe_Defs.Look(ref removedTrait, "removedTrait");
            Scribe_Values.Look(ref removedTraitDegree, "removedTraitDegree", 0);
        }

        private void OpenCitationDialog()
        {
            Find.WindowStack.Add(new Dialog_WriteCitation(this));
        }

        public override string LabelNoCount
        {
            get
            {
                if (BiocodeComp is not { Biocoded: true, CodedPawn: not null }) return base.LabelNoCount;
                if (field != null && _cachedPawn == BiocodeComp.CodedPawn) return field;
                    
                _cachedPawn = BiocodeComp.CodedPawn;

                var cleanLabel = GenLabel.ThingLabel(this.def, this.Stuff, 1);
                if (AllComps != null)
                {
                    foreach (var comp in AllComps)
                    {
                        if (comp is CompBiocodable) continue; 
                        cleanLabel = comp.TransformLabel(cleanLabel);
                    }
                }
                var sb = new StringBuilder();
                sb.Append(_cachedPawn.LabelShort);
                sb.Append("'s");
                sb.Append(' ');
                sb.Append(cleanLabel);
                field = sb.ToString();
                return field;
            }
        } = null;
            
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
                yield return gizmo;

            var awardCeremonyBtn = new Command_Action
            {
                defaultLabel = "FeatherMedals_AwardCeremony".Translate(),
                defaultDesc = "FeatherMedals_CeremonyDesc".Translate(),
                icon = this.def.uiIcon, 
                action = StartMedalRitual
            };
                
            // first failing reason wins
            if (BiocodeComp.Biocoded)
                awardCeremonyBtn.Disable("FeatherMedals_DisabledAlreadyAwarded".Translate());
            else if (IsCeremonyInProgress())
                awardCeremonyBtn.Disable("FeatherMedals_DisabledCeremonyInProgress".Translate());
            else if (!ColonyHasPresenter(out _))
                awardCeremonyBtn.Disable("FeatherMedals_DisabledRequiresPresenter".Translate());

            yield return awardCeremonyBtn;
                
            var citationBtn = new Command_Action
            {
                defaultLabel = citation.NullOrEmpty() ? "FeatherMedals_WriteCitation".Translate() : "FeatherMedals_EditCitation".Translate(),
                defaultDesc = "FeatherMedals_CitationDesc".Translate(),
                icon = TrophyTextures.CitationIcon,
                action = OpenCitationDialog
            };
                
            yield return citationBtn;
        }

        private bool IsCeremonyInProgress()
        {
            var lords = (Map ?? MapHeld)?.lordManager.lords;
            if (lords == null)
                return false;
            
            for (var i = 0; i < lords.Count; i++)
            {
                if (lords[i].LordJob is LordJob_Ritual ritual && ritual.selectedTarget.Thing == this)
                    return true;
            }
            return false;
        }

        private bool ColonyHasPresenter(out Pawn result)
        {
            result = null;
            if (this.Map == null) return false;
            foreach (var p in this.Map.mapPawns.FreeColonistsSpawned)
            {
                if (PreceptDefOf.IsSpeaker(p))
                {
                    result = p;
                    return true; 
                }
            }
            return false;
        }
            
        public string MedalLabel
        {
            get
            {
                var cleanLabel = GenLabel.ThingLabel(this.def, this.Stuff, 1);
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleanLabel);
            }
        }

        private void StartMedalRitual()
        {
            if (!ColonyHasPresenter(out Pawn presenter)) return;
            var pattern = FeatherMedalDefOf.FeatherMedal_AwardTrophyPattern;

            var safeMap = this.Map ?? this.MapHeld;
            if (safeMap == null) return;

            var ritualTarget = new TargetInfo(this);

            // A real precept: it lives, and is saved, in the ideo, so the running ritual can find it again after a reload
            var ritual = GetCeremonyPrecept(presenter.Ideo);
            var ceremonyName = "FeatherMedals_AwardCeremony".Translate();
                
            Dialog_BeginRitual.ActionCallback startAction = delegate (RitualRoleAssignments assignments)
            {
                // the dialog can sit open while another one is confirmed
                if (IsCeremonyInProgress())
                {
                    Messages.Message("FeatherMedals_DisabledCeremonyInProgress".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }
                var lordJob = new LordJob_Ritual(
                    selectedTarget: ritualTarget,
                    ritual: ritual,
                    obligation: null,
                    allStages: pattern.ritualBehavior.stages,
                    assignments: assignments,
                    organizer: null,
                    spotOverride: null
                );
                LordMaker.MakeNewLord(Faction.OfPlayer, lordJob, safeMap, assignments.Participants);
                return true; 
            };

            var outcomeDef = pattern.ritualOutcomeEffect;

            Find.WindowStack.Add(new Dialog_BeginRitual(
                ritualLabel: ceremonyName,
                ritual: ritual,       
                target: ritualTarget,     
                map: safeMap,             
                action: startAction,      
                organizer: null,
                obligation: null,         
                filter: null,
                okButtonText: "FeatherMedals_BeginCeremony".Translate(),
                requiredPawns: null,
                forcedForRole: null,
                outcomeDef        
            ));
        }
        
        private static Precept_Ritual GetCeremonyPrecept(Ideo ideo)
        {
            var def = FeatherMedalDefOf.FeatherMedals_MedalCeremonyPrecept;
            if (ideo.GetPrecept(def) is Precept_Ritual existing)
                return existing;

            // game not reloaded since the mod was added, do what vanilla does on load
            var created = (Precept_Ritual)PreceptMaker.MakePrecept(def);
            ideo.AddPrecept(created, init: true, fillWith: def.ritualPatternBase);
            return created;
        }

        public string GetAwardedByLabel()
        {
            if (awardedBy == null)
                return null;
            return awardedBy.Dead
                ? "FeatherMedals_Inspector_PresentedBy_Dead".Translate(awardedBy.Named("PAWN"))
                : "FeatherMedals_Inspector_PresentedBy".Translate(awardedBy.Named("PAWN"));
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            foreach (StatDrawEntry specialDisplayStat in base.SpecialDisplayStats())
                yield return specialDisplayStat;
            
            var ext = def.GetModExtension<TrophyExtension>();

            if (ext != null)
            {
                if (ext.addsTraits != null)
                    foreach (var trait in ext.addsTraits)
                    {
                        yield return new StatDrawEntry(
                            StatCategoryDefOf.Apparel,
                            (string)"FeatherMedals_StatDraw_AddTrait".Translate(),
                            trait.Label.CapitalizeFirst(),
                            (string)"FeatherMedals_StatDraw_AddTrait_Desc".Translate(trait.Label.Named("TRAIT")), 3752);
                    }
                
                if (ext.removesTraits != null)
                    foreach (var trait in ext.removesTraits)
                    {
                        yield return new StatDrawEntry(
                            StatCategoryDefOf.Apparel,
                            (string)"FeatherMedals_StatDraw_RemoveTrait".Translate(),
                            trait.Label.CapitalizeFirst(),
                            (string)"FeatherMedals_StatDraw_RemoveTrait_Desc".Translate(trait.Label.Named("TRAIT")), 3753);
                    }
            }
            
            
            
        }
        
        public override string GetInspectString()
        {
            var sb = new StringBuilder(base.GetInspectString());

            // Awarded to (biocoded pawn)
            if (BiocodeComp is { Biocoded: true, CodedPawn: not null })
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append("FeatherMedals_Inspector_AwardedTo".Translate(BiocodeComp.CodedPawn.Named("PAWN")));
            }

            // Awarded by
            if (awardedBy != null)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(GetAwardedByLabel());
            }

            // Date
            if (awardedTick >= 0)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append("FeatherMedals_Inspector_AwardedDate".Translate());
                sb.Append(GenDate.DateFullStringAt(
                    GenDate.TickGameToAbs(awardedTick),
                    Find.WorldGrid.LongLatOf(
                        Wearer?.Map?.Tile ?? Find.CurrentMap?.Tile ?? 0)));
            }

            // Ceremony quality
            if (ceremonyQuality >= 0)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append("FeatherMedals_Inspector_AwardedCeremonyQuality".Translate());
                sb.Append(AdorningQuality.GetQualityLabel(ceremonyQuality).CapitalizeFirst());
            }

            // Citation
            if (!citation.NullOrEmpty())
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append('"');
                sb.Append(citation);
                sb.Append('"');
            }

            return sb.ToString();
        }

    }
}