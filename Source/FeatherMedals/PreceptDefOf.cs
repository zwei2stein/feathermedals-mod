using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FeatherMedals;

[DefOf]
public class PreceptDefOf
{
   
    public static bool IsSpeaker(Pawn pawn)
    {
        var role = pawn.Ideo?.GetRole(pawn);
        if (role == null)
            return false;
        List<PreceptDef> speakerRoleList =
        [
            RimWorld.PreceptDefOf.IdeoRole_Leader,
            RimWorld.PreceptDefOf.IdeoRole_Moralist,
            IdeoRole_ShootingSpecialist,
            IdeoRole_MeleeSpecialist,
            IdeoRole_ResearchSpecialist,
            IdeoRole_MedicalSpecialist
        ];

#if DEBUG
        Log.Message(pawn.Name);
        Log.Message(role);
        Log.Message(role.def);
        Log.Message(IdeoRole_ShootingSpecialist);
#endif

        return speakerRoleList.Contains(role.def);
    }
    
    [MayRequireIdeology]
    public static PreceptDef IdeoRole_ShootingSpecialist;
    
    [MayRequireIdeology]
    public static PreceptDef IdeoRole_MeleeSpecialist;

    [MayRequireIdeology]
    public static PreceptDef IdeoRole_ResearchSpecialist;

    [MayRequireIdeology]
    public static PreceptDef IdeoRole_MedicalSpecialist;
        
    static PreceptDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (PreceptDefOf));
}