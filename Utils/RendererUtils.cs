using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NifRenderer.Utils
{
    public static class RendererUtils
    {
        public static Vector3 GetTriNormal(
            VertexPositionColorNormalTexture v1,
            VertexPositionColorNormalTexture v2,
            VertexPositionColorNormalTexture v3)
        {
            var side1 = v1.Position - v3.Position;

            var side2 = v1.Position - v2.Position;

            return Vector3.Normalize(Vector3.Cross(side1, side2));
        }
    }
}
