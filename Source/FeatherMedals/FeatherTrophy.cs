using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FeatherMedals
{
    public class FeatherTrophy : Apparel
    {
        public CompBiocodable BiocodeComp => field ??= this.GetComp<CompBiocodable>();
        private Pawn AdornedPawn => BiocodeComp?.CodedPawn;
        private string cachedLabelPawnName;
            
        public string tale;
        public int ceremonyQuality = -1; // -1 = not yet adorned
        public RememberedPawn adornedBy;
        
        public EventTime adorned; // null = not yet adorned

        public TraitDef addedTrait = null;
        public int addedTraitDegree = 0;
        public TraitDef removedTrait = null;
        public int removedTraitDegree = 0;
        public int decoratedDegree = -1; // Decorated trait degree, -1 = unchanged
            
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tale, "tale");
            Scribe_Values.Look(ref ceremonyQuality, "ceremonyQuality", -1);
            Scribe_Deep.Look(ref adornedBy, "adornedBy");
            
            Scribe_Deep.Look(ref adorned, "adorned");

            Scribe_Defs.Look(ref addedTrait, "addedTrait");
            Scribe_Values.Look(ref addedTraitDegree, "addedTraitDegree", 0);
            Scribe_Defs.Look(ref removedTrait, "removedTrait");
            Scribe_Values.Look(ref removedTraitDegree, "removedTraitDegree", 0);
            Scribe_Values.Look(ref decoratedDegree, "decoratedDegree", -1);
        }

        private void OpenTaleDialog()
        {
            Find.WindowStack.Add(new Dialog_WriteTale(this));
        }

        public bool HasTale => !tale.NullOrEmpty();

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
                field = "TribalTrophies_Trophy_Misc_AdornedLabel".Translate(
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

            var adornCeremonyGizmo = new Command_Action
            {
                defaultLabel = "TribalTrophies_AdorningCeremony_Common_CeremonyLabel".Translate(),
                defaultDesc = "TribalTrophies_AdorningCeremony_Gizmo_CeremonyDesc".Translate(),
                icon = this.def.uiIcon, 
                action = StartTrophyAdorningRitual
            };
            
            // first failing reason wins
            if (BiocodeComp is { Biocoded: true })
                adornCeremonyGizmo.Disable("TribalTrophies_AdorningCeremony_Gizmo_DisabledAlreadyAdorned".Translate());
            else if (IsCeremonyInProgress())
                adornCeremonyGizmo.Disable("TribalTrophies_AdorningCeremony_Common_CeremonyInProgress".Translate());
            else if (!Spawned)
                adornCeremonyGizmo.Disable("TribalTrophies_AdorningCeremony_Gizmo_DisabledNotOnGround".Translate());
            else if (!ColonyHasPresenter(out _))
                adornCeremonyGizmo.Disable("TribalTrophies_AdorningCeremony_Gizmo_DisabledRequiresPresenter".Translate(SpeakerRolesUtil.SpeakerRolesLabel.Named("SPEAKERS")));
            
            yield return adornCeremonyGizmo;
                
            var taleGizmo = new Command_Action
            {
                defaultLabel = !HasTale ? "TribalTrophies_AdorningCeremony_Gizmo_WriteTale".Translate() : "TribalTrophies_AdorningCeremony_Gizmo_EditTale".Translate(),
                defaultDesc = "TribalTrophies_AdorningCeremony_Gizmo_TaleDesc".Translate(),
                icon = TrophyTextures.TaleIcon,
                action = OpenTaleDialog
            };

            // the tale is part of the ceremony, it is fixed once the trophy is adorned
            if (BiocodeComp is { Biocoded: true })
                taleGizmo.Disable("TribalTrophies_AdorningCeremony_Gizmo_DisabledTaleLocked".Translate());
                
            yield return taleGizmo;
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
            
        public string TrophyLabel
        {
            get
            {
                var cleanLabel = GenLabel.ThingLabel(this.def, this.Stuff, 1);
                return cleanLabel.CapitalizeFirst(this.def);
            }
        }

        private void StartTrophyAdorningRitual()
        {
            if (!ColonyHasPresenter(out Pawn presenter)) return;
            var pattern = TribalTrophiesDefOf.TribalTrophies_AdorningCeremony_Pattern;

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
            var def = TribalTrophiesDefOf.TribalTrophies_AdorningCeremonyPrecept;
            if (ideo.GetPrecept(def) is Precept_Ritual existing)
                return existing;

            // game not reloaded since the mod was added, do what vanilla does on load
            var created = (Precept_Ritual)PreceptMaker.MakePrecept(def);
            ideo.AddPrecept(created, init: true, fillWith: def.ritualPatternBase);
            return created;
        }

        public string GetAdornedByLabel()
        {
            if (adornedBy == null)
                return null;
            return "TribalTrophies_AdorningCeremony_Common_AdornedBy".Translate(adornedBy.Name.Named("PAWN"));
        }
        
        public string GetAdornedDateLabel()
        {
            return adorned?.DateLabel;
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
                    TribalTrophiesDefOf.TribalTrophies_Decorated.DataAtDegree(this.decoratedDegree).label.CapitalizeFirst().Named("TRAIT")
                        ).CapitalizeFirst().Resolve();
            return "TribalTrophies_AdorningCeremony_Common_DecoratedGained".Translate(
                this.AdornedPawn.Named("PAWN"),
                TribalTrophiesDefOf.TribalTrophies_Decorated.DataAtDegree(this.decoratedDegree).label.CapitalizeFirst().Named("TRAIT")).Resolve();
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
                if (TribalTrophiesMod.Settings.TrophyDynamicTraits)
                {
                    if (ext.addsTraits != null)
                        foreach (var trait in ext.addsTraits)
                        {
                            yield return new StatDrawEntry(
                                StatCategoryDefOf.Apparel,
                                (string)"TribalTrophies_Trophy_InfoCard_MayGrant".Translate(),
                                trait.Label.CapitalizeFirst(),
                                (string)"TribalTrophies_Trophy_InfoCard_MayGrant_Desc".Translate(trait.Label.Named("TRAIT")),
                                3752);
                        }
                    if (ext.removesTraits != null)
                        foreach (var trait in ext.removesTraits)
                        {
                            yield return new StatDrawEntry(
                                StatCategoryDefOf.Apparel,
                                (string)"TribalTrophies_Trophy_InfoCard_MayRemove".Translate(),
                                trait.Label.CapitalizeFirst(),
                                (string)"TribalTrophies_Trophy_InfoCard_MayRemove_Desc".Translate(trait.Label.Named("TRAIT")),
                                3753);
                        }
                }
            }
            
            
            
        }
        
        public override string GetInspectString()
        {
            var sb = new StringBuilder(base.GetInspectString());

            // adorned to (biocoded pawn)
            if (BiocodeComp is { Biocoded: true, CodedPawn: not null })
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append("TribalTrophies_AdorningCeremony_Common_Honoring".Translate(BiocodeComp.CodedPawn.Named("PAWN")));
            }

            if (adornedBy != null)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(GetAdornedByLabel());
            }

            // Date
            var dateLabel = GetAdornedDateLabel();
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

            if (HasTale)
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                sb.Append($"\"{tale}\"");
            }

            return sb.ToString();
        }

    }
}