using System.Linq;
using UnityEngine;
using Verse;

namespace FeatherMedals;

public class Graphic_RuntimeTexture : Graphic
{
    private readonly Material[] mats = new Material[4];
    
    public Graphic_RuntimeTexture(Texture2D texture, GraphicData graphicData)
        : this(graphicData)
    {
        var mat = MakeMaterial(texture);
        for (int i = 0; i < mats.Length; i++)
            mats[i] = mat;
    }

    public Graphic_RuntimeTexture(Texture2D[] textures, GraphicData graphicData)
        : this(graphicData)
    {
        foreach (var facing in Rot4.AllRotations)
            mats[facing.AsInt] = MakeMaterial(textures[facing.AsInt]);
    }

    private Graphic_RuntimeTexture(GraphicData graphicData)
    {
        data = graphicData;
        drawSize = graphicData != null ? graphicData.drawSize : Vector2.one;
    }

    private static Material MakeMaterial(Texture2D texture)
    {
        return new Material(ShaderDatabase.Cutout)
        {
            name = "RuntimeTextureMaterial",
            mainTexture = texture,
            color = Color.white
        };
    }

    public override Material MatSingle => mats[Rot4.South.AsInt];
    public override Material MatNorth => mats[Rot4.North.AsInt];
    public override Material MatEast => mats[Rot4.East.AsInt];
    public override Material MatSouth => mats[Rot4.South.AsInt];
    public override Material MatWest => mats[Rot4.West.AsInt];

    // every facing has its own texture, nothing is drawn mirrored
    public override bool WestFlipped => false;

    public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
    {
        return this;
    }

    public void Release()
    {
        // a single texture graphic has the same material at every facing
        foreach (var mat in mats.Distinct())
        {
            if (mat != null)
                UnityEngine.Object.Destroy(mat);
        }
        for (int i = 0; i < mats.Length; i++)
            mats[i] = null;
    }
}