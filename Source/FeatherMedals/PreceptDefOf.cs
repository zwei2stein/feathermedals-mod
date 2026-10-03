using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FeatherMedals;

[DefOf]
public class PreceptDefOf
{
   
    private static List<PreceptDef> speakerRoles;
    private static string speakerRolesLabel;

    private static List<PreceptDef> SpeakerRoles => speakerRoles ??= new List<PreceptDef>
    {
        RimWorld.PreceptDefOf.IdeoRole_Leader,
        RimWorld.PreceptDefOf.IdeoRole_Moralist,
        IdeoRole_ShootingSpecialist,
        IdeoRole_MeleeSpecialist,
        IdeoRole_ResearchSpecialist,
        IdeoRole_MedicalSpecialist
    };

    public static string SpeakerRolesLabel => speakerRolesLabel ??=
        string.Join(", ", SpeakerRoles.Select(role => role.LabelCap.Resolve()));

    public static bool IsSpeaker(Pawn pawn)
    {
        var role = pawn.Ideo?.GetRole(pawn);
        if (role == null)
            return false;

        return SpeakerRoles.Contains(role.def);
    }
    
    public static PreceptDef IdeoRole_ShootingSpecialist;
    
    public static PreceptDef IdeoRole_MeleeSpecialist;
    
    public static PreceptDef IdeoRole_ResearchSpecialist;
    
    public static PreceptDef IdeoRole_MedicalSpecialist;
        
    static PreceptDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (PreceptDefOf));
}