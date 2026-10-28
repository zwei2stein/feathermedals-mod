using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public static class AdorningQuality
{
    public const float AttendanceWeight = 0.4f;
    public const float RoomWeight = 0.4f;
    public const float TaleWeight = 0.2f;
    private const float TrophyQualityWeightStep = 0.1f;
    private const float MaxRoomImpressiveness = 170f;
    
    public static float MaxTrophyBonus => ((int)QualityCategory.Legendary - (int)QualityCategory.Normal) * TrophyQualityWeightStep;
    public static float MaxTrophyPenalty => ((int)QualityCategory.Normal - (int)QualityCategory.Awful) * TrophyQualityWeightStep;
    
    public static float GetTrophyQualityBonus(Thing trophy)
    {
        return trophy.TryGetQuality(out var quality)
            ? ((int)quality - (int)QualityCategory.Normal) * TrophyQualityWeightStep
            : 0f;
    }
    
    public static float GetQualityScore(int attendees, int totalColonists, float roomImpressiveness, bool hasTale, float trophyQualityBonus)
    {
        // Attendance: 0-1 based on ratio, capped at 1.0
        var attendanceScore = GetAttendanceScore(attendees, totalColonists);

        // Room impressiveness: mapped from 0-170 (max in vanilla) to 0-1
        // Somewhat impressive (25) = ~0.15, Very impressive (50) = ~0.29
        // Extremely impressive (85) = ~0.5, Unbelievably impressive (170) = 1.0
        var roomScore = GetRoomScore(roomImpressiveness);
        
        var taleScore = GetTaleScore(hasTale);

        return attendanceScore
               + roomScore
               + taleScore
               + trophyQualityBonus;
    }

    public static float GetAttendanceRatio(int attendees, int totalColonists)
    {
        return totalColonists > 0
            ? Mathf.Clamp01((float)attendees / totalColonists)
            : 0f;
    }

    public static float GetAttendanceScore(int attendees, int totalColonists)
        => GetAttendanceRatio(attendees, totalColonists) * AttendanceWeight;

    public static float GetRoomScore(float roomImpressiveness)
        => Mathf.Clamp01(roomImpressiveness / MaxRoomImpressiveness) * RoomWeight;

    public static float GetTaleScore(bool hasTale)
        => hasTale ? TaleWeight : 0f;

    // spectators + presenter
    public static int CountAttendees(IEnumerable<Pawn> present, Pawn awardee)
    {
        var count = 0;
        foreach (var pawn in present)
        {
            if (pawn != awardee)
                count++;
        }
        return count;
    }

    // free adult colonists - awardee
    public static int CountPossibleAttendees(Map map, Pawn awardee)
    {
        if (map == null)
            return 0;
        var count = 0;
        foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
        {
            if (pawn == awardee || !pawn.DevelopmentalStage.Adult())
                continue;
            count++;
        }
        return count;
    }
    
    public static int GetStageIndex(float qualityScore)
    {
        if (qualityScore >= 0.8f) return 3;  // Legendary
        if (qualityScore >= 0.5f) return 2;  // Grand
        if (qualityScore >= 0.25f) return 1; // Decent
        return 0;                            // Poor
    }

    public static string GetQualityLabel(int stageIndex) =>
        stageIndex switch
        {
            3 => "TribalTrophies_AdorningCeremony_Common_Quality_Legendary".Translate(),
            2 => "TribalTrophies_AdorningCeremony_Common_Quality_Grand".Translate(),
            1 => "TribalTrophies_AdorningCeremony_Common_Quality_Decent".Translate(),
            _ => "TribalTrophies_AdorningCeremony_Common_Quality_Poor".Translate()
        };
    
    public static float GetRoomImpressiveness(TargetInfo target)
    {
        if (!target.HasThing && !target.Cell.IsValid) return 0f;
        var map = target.Map;
        if (map == null) return 0f;
        var cell = target.Cell;
        var room = cell.GetRoom(map);
        if (room == null || room.PsychologicallyOutdoors) return 0f;
        return room.GetStat(RoomStatDefOf.Impressiveness);
    }
}