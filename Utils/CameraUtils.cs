using Microsoft.Xna.Framework;
using System;

namespace NifRenderer.Utils
{
    public static class CameraUtils
    {
        public static Vector3 DirectionOf(float yaw, float pitch)
        {
            var cosPitch = MathF.Cos(pitch);

            return new Vector3(
                cosPitch * MathF.Cos(yaw),
                cosPitch * MathF.Sin(yaw),
                MathF.Sin(pitch));
        }

        public static Matrix ViewMatrix(float yaw, float pitch, Vector3 target, float distance)
        {
            var forward = DirectionOf(yaw, pitch);

            var position = target + forward * distance;

            var right = new Vector3(-MathF.Sin(yaw), MathF.Cos(yaw), 0f);

            var up = Vector3.Cross(forward, right);

            return new Matrix(
                right.X, up.X, forward.X, 0f,
                right.Y, up.Y, forward.Y, 0f,
                right.Z, up.Z, forward.Z, 0f,
                -Vector3.Dot(right, position), -Vector3.Dot(up, position), -Vector3.Dot(forward, position), 1f);
        }
    }
}
