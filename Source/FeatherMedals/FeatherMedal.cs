using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace FeatherMedals
{
    public class FeatherMedal : Apparel
    {
        public CompBiocodable BiocodeComp => field ??= this.GetComp<CompBiocodable>();
        private Pawn AdornedPawn => BiocodeComp?.CodedPawn;
        private string cachedLabelPawnName;
            
        public string citation;
        public int ceremonyQuality = -1; // -1 = not yet awarded
        public RememberedPawn awardedBy;
        
        public int awardedTick = -1;
        public PlanetTile awardedTile = PlanetTile.Invalid; // ceremony map tile for date
        public bool HasAwardTick => awardedTick != -1;

        public TraitDef addedTrait = null;
        public int addedTraitDegree = 0;
        public TraitDef removedTrait = null;
        public int removedTraitDegree = 0;
        public int decoratedDegree = -1; // Decorated trait degree, -1 = unchanged
            
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref citation, "citation");
            Scribe_Values.Look(ref ceremonyQuality, "ceremonyQuality", -1);
            Scribe_Deep.Look(ref awardedBy, "awardedBy");
            
            Scribe_Values.Look(ref awardedTick, "awardedTick", -1);
            Scribe_Values.Look(ref awardedTile, "awardedTile", PlanetTile.Invalid);
            
            Scribe_Defs.Look(ref addedTrait, "addedTrait");
            Scribe_Values.Look(ref addedTraitDegree, "addedTraitDegree", 0);
            Scribe_Defs.Look(ref removedTrait, "removedTrait");
            Scribe_Values.Look(ref removedTraitDegree, "removedTraitDegree", 0);
            Scribe_Values.Look(ref decoratedDegree, "decoratedDegree", -1);
        }

        private void OpenCitationDialog()
        {
            Find.WindowStack.Add(new Dialog_WriteCitation(this));
        }

        public bool HasCitation => !citation.NullOrEmpty();

        public override string LabelNoCount
        {
            get
            {
                if (BiocodeComp is not { Biocoded: true, CodedPawn: not null }) return base.LabelNoCount;

                var pawnName = AdornedPawn.LabelShort;
                if (field != null && cachedLabelPawnName == pawnName) return field;

                var cleanLabel = GenLabel.ThingLabel(this.def, this.Stuff, 1);
                if (AllComps != null)
                {
                    foreach (var comp in AllComps)
                    {
                        if (comp is CompBiocodable) continue;
                        cleanLabel = comp.TransformLabel(cleanLabel);
                    }
                }
                field = "FeatherMedals_BiocodedLabel".Translate(
                    AdornedPawn.Named("PAWN"),
                    cleanLabel.Named("TROPHY")).Resolve();
                cachedLabelPawnName = pawnName;
                return field;
            }
        } = null;
            
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
                yield return gizmo;

            var awardCeremonyBtn = new Command_Action
            {
                defaultLabel = "TribalTrophies_AdorningCeremony_Common_CeremonyLabel".Translate(),
                defaultDesc = "TribalTrophies_AdorningCeremony_Gizmo_CeremonyDesc".Translate(),
                icon = this.def.uiIcon, 
                action = StartMedalRitual
            };
            
            // first failing reason wins
            if (BiocodeComp is { Biocoded: true })
                awardCeremonyBtn.Disable("TribalTrophies_AdorningCeremony_Gizmo_DisabledAlreadyAdorned".Translate());
            else if (IsCeremonyInProgress())
                awardCeremonyBtn.Disable("TribalTrophies_AdorningCeremony_Common_CeremonyInProgress".Translate());
            else if (!Spawned)
                awardCeremonyBtn.Disable("TribalTrophies_AdorningCeremony_Gizmo_DisabledNotOnGround".Translate());
            else if (!ColonyHasPresenter(out _))
                awardCeremonyBtn.Disable("TribalTrophies_AdorningCeremony_Gizmo_DisabledRequiresPresenter".Translate(SpeakerRolesUtil.SpeakerRolesLabel.Named("SPEAKERS")));
            
            yield return awardCeremonyBtn;
                
            var citationBtn = new Command_Action
            {
                defaultLabel = !HasCitation ? "TribalTrophies_AdorningCeremony_Gizmo_WriteTale".Translate() : "TribalTrophies_AdorningCeremony_Gizmo_EditTale".Translate(),
                defaultDesc = "TribalTrophies_AdorningCeremony_Gizmo_TaleDesc".Translate(),
                icon = TrophyTextures.CitationIcon,
                action = OpenCitationDialog
            };

            // the tale is part of the ceremony, it is fixed once the trophy is adorned
            if (BiocodeComp is { Biocoded: true })
                citationBtn.Disable("TribalTrophies_AdorningCeremony_Gizmo_DisabledTaleLocked".Translate());
                
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
                if (SpeakerRolesUtil.IsSpeaker(p) && !HonoringWarriorsUtil.ForbidsCeremony(p))
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
                return cleanLabel.CapitalizeFirst(this.def);
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
            var ceremonyName = "TribalTrophies_AdorningCeremony_Common_CeremonyLabel".Translate();
            
            Dialog_BeginRitual.PawnFilter filter = (pawn, voluntary, allowOtherIdeos) => pawn.GetLord() == null;
                
            Dialog_BeginRitual.ActionCallback startAction = delegate (RitualRoleAssignments assignments)
            {
                // the dialog can sit open while another one is confirmed
                if (IsCeremonyInProgress())
                {
                    Messages.Message("TribalTrophies_AdorningCeremony_Common_CeremonyInProgress".Translate(), MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                // TryExecuteOn returns silently when blocked, so check first and keep the dialog open
                var blocker = ritual.behavior.CanStartRitualNow(ritualTarget, ritual, forcedForRole: assignments.ForcedRolesForReading);
                if (!blocker.NullOrEmpty())
                {
                    Messages.Message(blocker, ritualTarget, MessageTypeDefOf.RejectInput, false);
                    return false;
                }

                // vanilla start: pulls pawns out of voluntary lords, ends jobs, makes the lord, PreparePawns, ResetCompDatas
                ritual.behavior.TryExecuteOn(ritualTarget, null, ritual, null, assignments, true);
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
                filter: filter,
                okButtonText: "TribalTrophies_AdorningCeremony_RitualDialog_Begin".Translate(),
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
            return "TribalTrophies_AdorningCeremony_Common_AdornedBy".Translate(awardedBy.Name.Named("PAWN"));
        }
        
        public string GetAwardedDateLabel()
        {
            if (!HasAwardTick)
                return null;
            var longLat = awardedTile.Valid ? Find.WorldGrid.LongLatOf(awardedTile) : Vector2.zero;
            return GenDate.DateFullStringAt(GenDate.TickGameToAbs(awardedTick), longLat);
        }

        public string GetAddedTraitLabel(bool terse = false)
        {
            if (addedTrait == null)
                return null;
            if (terse || this.AdornedPawn == null)
                return "TribalTrophies_AdorningCeremony_Common_TraitAdded_Short".Translate(
                    this.addedTrait.DataAtDegree(this.addedTraitDegree).label.Named("TRAIT")
                        ).CapitalizeFirst().Resolve();
            return "TribalTrophies_AdorningCeremony_Common_TraitGained".Translate(
                this.AdornedPawn.Named("PAWN"),
                this.addedTrait.DataAtDegree(this.addedTraitDegree).label.Named("TRAIT")).Resolve();
        }

        public string GetRemovedTraitLabel(bool terse = false)
        {
            if (removedTrait == null)
                return null;
            if (terse || this.AdornedPawn == null)
                return "TribalTrophies_AdorningCeremony_Common_TraitRemoved_Short".Translate(
                    this.removedTrait.DataAtDegree(this.removedTraitDegree).label.Named("TRAIT")
                        ).CapitalizeFirst().Resolve();
            return "TribalTrophies_AdorningCeremony_Common_TraitShed".Translate(
                this.AdornedPawn.Named("PAWN"),
                this.removedTrait.DataAtDegree(this.removedTraitDegree).label.Named("TRAIT")).Resolve();
        }
        
        public string GetDecoratedTraitLabel(bool terse = false)
        {
            if (decoratedDegree < 0)
                return null;
            if (terse || this.AdornedPawn == null)
                return "TribalTrophies_AdorningCeremony_Common_TraitAdded_Short".Translate(
                    FeatherMedalDefOf.FeatherMedals_Decorated.DataAtDegree(this.decoratedDegree).label.CapitalizeFirst().Named("TRAIT")
                        ).CapitalizeFirst().Resolve();
            return "TribalTrophies_AdorningCeremony_Common_DecoratedGained".Translate(
                this.AdornedPawn.Named("PAWN"),
                FeatherMedalDefOf.FeatherMedals_Decorated.DataAtDegree(this.decoratedDegree).label.CapitalizeFirst().Named("TRAIT")).Resolve();
        }

        public string GetTraitChangeText()
        {
            var lines = new List<string>();
            if (this.decoratedDegree >= 0) lines.Add(GetDecoratedTraitLabel());
            if (this.addedTrait != null) lines.Add(GetAddedTraitLabel());
            if (this.removedTrait != null) lines.Add(GetRemovedTraitLabel());
            return lines.Count == 0 ? null : string.Join("\n", lines);
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            foreach (StatDrawEntry specialDisplayStat in base.SpecialDisplayStats())
                yield return specialDisplayStat;
            
            var ext = def.GetModExtension<TrophyExtension>();

            if (ext != null)
            {
                if (MedalMod.Settings.TrophyDynamicTraits)
                {
                    if (ext.addsTraits != null)
                        foreach (var trait in ext.addsTraits)
                        {
                            yield return new StatDrawEntry(
                                StatCategoryDefOf.Apparel,
                                (string)"FeatherMedals_StatDraw_AddTrait".Translate(),
                                trait.Label.CapitalizeFirst(),
                                (string)"FeatherMedals_StatDraw_AddTrait_Desc".Translate(trait.Label.Named("TRAIT")),
                                3752);
                        }
                    if (ext.removesTraits != null)
                        foreach (var trait in ext.removesTraits)
                        {
                            yield return new StatDrawEntry(
                                StatCategoryDefOf.Apparel,
                                (string)"FeatherMedals_StatDraw_RemoveTrait".Translate(),
                                trait.Label.CapitalizeFirst(),
                                (string)"FeatherMedals_StatDraw_RemoveTrait_Desc".Translate(trait.Label.Named("TRAIT")),
                                3753);
                        }
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
                sb.Append("TribalTrophies_AdorningCeremony_Common_Honoring".Translate(BiocodeComp.CodedPawn.Named("PAWN")));
            }

            // Awarded by
            if (awardedBy != null)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(GetAwardedByLabel());
            }

            // Date
            var dateLabel = GetAwardedDateLabel();
            if (dateLabel != null)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append("TribalTrophies_AdorningCeremony_Inspect_AdornedOn".Translate(dateLabel.Named("DATE")));
            }

            // Ceremony quality
            if (ceremonyQuality >= 0)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append("TribalTrophies_AdorningCeremony_Inspect_CeremonyQuality".Translate(
                    AdorningQuality.GetQualityLabel(ceremonyQuality).CapitalizeFirst().Named("QUALITY")));
            }

            // Citation
            if (HasCitation)
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