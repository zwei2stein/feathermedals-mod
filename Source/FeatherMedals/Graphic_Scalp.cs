using UnityEngine;
using Verse;

namespace FeatherMedals
{
    // Graphic over a runtime texture. Material is private to this graphic (not in MaterialPool), freed by Release.
    public class Graphic_Scalp : Graphic
    {
        private Material mat;

        public Graphic_Scalp(Texture2D texture, GraphicData graphicData)
        {
            data = graphicData;
            drawSize = graphicData != null ? graphicData.drawSize : Vector2.one;
            mat = new Material(ShaderDatabase.Cutout)
            {
                name = "ScalpMaterial",
                mainTexture = texture,
                color = Color.white
            };
        }

        public override Material MatSingle => mat;

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return this;
        }

        public void Release()
        {
            if (mat != null)
            {
                Object.Destroy(mat);
                mat = null;
            }
        }
    }
}