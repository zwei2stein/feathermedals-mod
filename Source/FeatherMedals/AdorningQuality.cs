using RimWorld;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public static class AdorningQuality
{
    private const float AttendanceWeight = 0.4f;
    private const float RoomWeight = 0.4f;
    private const float CitationWeight = 0.2f;

    /// <summary>
    /// Returns a quality score from 0.0 to 1.0 based on attendance, room, and citation.
    /// </summary>
    public static float GetQualityScore(int attendees, int totalColonists, float roomImpressiveness, bool hasCitation)
    {
        // Attendance: 0-1 based on ratio, capped at 1.0
        var attendanceRatio = totalColonists > 0
            ? Mathf.Clamp01((float)attendees / totalColonists)
            : 0f;

        // Room impressiveness: mapped from 0-170 (max in vanilla) to 0-1
        // Somewhat impressive (25) = ~0.15, Very impressive (50) = ~0.29
        // Extremely impressive (85) = ~0.5, Unbelievably impressive (170) = 1.0
        var roomScore = Mathf.Clamp01(roomImpressiveness / 170f);

        // Citation: binary
        var citationScore = hasCitation ? 1f : 0f;

        return (attendanceRatio * AttendanceWeight)
               + (roomScore * RoomWeight)
               + (citationScore * CitationWeight);
    }

    /// <summary>
    /// Returns 0-3 stage index from a quality score.
    /// </summary>
    public static int GetStageIndex(float qualityScore)
    {
        if (qualityScore >= 0.8f) return 3;  // Legendary
        if (qualityScore >= 0.5f) return 2;  // Grand
        if (qualityScore >= 0.25f) return 1; // Decent
        return 0;                            // Poor
    }

    // Convenience overload for backward compat
    public static int GetStageIndex(int attendees, int totalColonists) => 
        GetStageIndex(GetQualityScore(attendees, totalColonists, 0f, false));

    public static string GetQualityLabel(int stageIndex) =>
        stageIndex switch
        {
            3 => "FeatherMedals_Quality_Legendary".Translate(),
            2 => "FeatherMedals_Quality_Grand".Translate(),
            1 => "FeatherMedals_Quality_Decent".Translate(),
            _ => "FeatherMedals_Quality_Poor".Translate()
        };

    /// <summary>
    /// Gets room impressiveness at a target position. Returns 0 if outdoors.
    /// </summary>
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