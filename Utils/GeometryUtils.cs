using System;
using Microsoft.Xna.Framework;

namespace NifRenderer.Utils
{
    public static class GeometryUtils
    {
        private const float DeterminantEpsilon = 1e-8f;

        private const float NearEpsilon = 1e-4f;

        public static bool RayTriangle(Vector3 origin, Vector3 direction, Vector3 a, Vector3 b, Vector3 c, out float distance)
        {
            distance = 0f;

            var edge1 = b - a;

            var edge2 = c - a;

            var cross = Vector3.Cross(direction, edge2);

            var determinant = Vector3.Dot(edge1, cross);

            if (MathF.Abs(determinant) < DeterminantEpsilon)
                return false;

            var inverse = 1f / determinant;

            var offset = origin - a;

            var u = Vector3.Dot(offset, cross) * inverse;

            if (u < 0f || u > 1f)
                return false;

            var q = Vector3.Cross(offset, edge1);

            var v = Vector3.Dot(direction, q) * inverse;

            if (v < 0f || u + v > 1f)
                return false;

            var t = Vector3.Dot(edge2, q) * inverse;

            if (t <= NearEpsilon)
                return false;

            distance = t;

            return true;
        }
    }
}
