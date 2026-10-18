using UnityEngine;
using Verse;

namespace FeatherMedals;

public static class TrophyLayout
{
    // Ellipse.
    // apex = CenterZ + RadiusZ
    private const float RadiusX = 0.32f;
    private const float RadiusZ = 0.30f;
    private const float AngleStepDeg = 18f;
    private const float MaxHalfArcDeg = 110f;
    private const float RingStep = 0.06f;

    // Fan.
    private const float MaxTiltDeg = 35f;

    private static void Slot(int index, int count, out int row, out float phiDeg)
    {
        var maxPerRow = MedalMod.Settings.MaxDisplayedTrophies;
        
        var scale = MedalMod.Settings.TrophyScale;
        var reverseIndex = (count - 1) - index;
        row = reverseIndex / maxPerRow;
        var col = reverseIndex % maxPerRow;
        var totalRows = (count + maxPerRow - 1) / maxPerRow;
        var inThisRow = maxPerRow;
        if (row == totalRows - 1 && count % maxPerRow != 0) inThisRow = count % maxPerRow;
        var slot = col + (maxPerRow - inThisRow) / 2f - (maxPerRow - 1) / 2f; // 0 = middle
        var stepDeg = AngleStepDeg * scale;
        var halfSpanDeg = (maxPerRow - 1) / 2f * stepDeg;
        if (halfSpanDeg > MaxHalfArcDeg) stepDeg *= MaxHalfArcDeg / halfSpanDeg;
        phiDeg = slot * stepDeg; // 0 = top of head
    }

    private static bool Mirrored(Rot4 facing) => facing == Rot4.West || facing == Rot4.North;

    public static Vector3 OffsetFor(int index, int count, Rot4 facing)
    {
        Slot(index, count, out var row, out var phiDeg);
        var ring = row * RingStep * MedalMod.Settings.TrophyScale;
        var phi = phiDeg * Mathf.Deg2Rad;
        var x = (RadiusX + ring) * Mathf.Sin(phi);
        var z = MedalMod.Settings.VerticalOffset + (RadiusZ + ring) * Mathf.Cos(phi);
        if (Mirrored(facing)) x = -x;
        return new Vector3(x, 0f, z);
    }

    /// Degrees around Y. Positive = top leans toward +x (clockwise from above).
    public static float TiltFor(int index, int count, Rot4 facing)
    {
        Slot(index, count, out _, out var phiDeg);
        var tilt = Mathf.Clamp(phiDeg * MedalMod.Settings.FanOut, -MaxTiltDeg, MaxTiltDeg);
        return Mirrored(facing) ? -tilt : tilt;
    }
}