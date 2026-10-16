using Microsoft.Xna.Framework;
using NiflySharp.Structs;

namespace NifRenderer.Utils
{
    public class MatrixUtils
    {
        public static Matrix From33(Matrix33 matrix)
        {
            var m = new Matrix
            {
                M11 = matrix.M11,
                M12 = matrix.M12,
                M13 = matrix.M13,

                M21 = matrix.M21,
                M22 = matrix.M22,
                M23 = matrix.M23,

                M31 = matrix.M31,
                M32 = matrix.M32,
                M33 = matrix.M33,

                M44 = 1f
            };

            return m;
        }
    }
}
