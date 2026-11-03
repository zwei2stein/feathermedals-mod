using UnityEngine;
using Verse;

namespace TribalTrophies;

public class Command_TrophyToggle : Command_Toggle
{
    public Color? backgroundTint;

    public override void DrawIcon(Rect rect, Material buttonMat, GizmoRenderParms parms)
    {
        if (backgroundTint.HasValue)
        {
            GUI.color = parms.lowLight ? backgroundTint.Value * LowLightBgColor : backgroundTint.Value;
            GenUI.DrawTextureWithMaterial(rect, parms.shrunk ? BGTextureShrunk : BGTexture, buttonMat);
            GUI.color = Color.white;
        }
        base.DrawIcon(rect, buttonMat, parms);
    }
}
