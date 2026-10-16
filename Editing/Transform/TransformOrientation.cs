using Microsoft.Xna.Framework;
using NifRenderer.Renderer.Scene;

namespace NifRenderer.Editing.Transform
{
    public enum Axis
    {
        X,
        Y,
        Z
    }

    public enum Orientation
    {
        Global,
        Local,
        View
    }

    public static class Orientations
    {
        public static Matrix Axes(Orientation orientation, Renderer.Renderer owner, TransformSet? targets)
        {
            switch (orientation)
            {
                case Orientation.View:

                    return ViewAxes(owner);

                case Orientation.Local:

                    return LocalAxes(targets);

                default:

                    return Matrix.Identity;
            }
        }

        public static Vector3 AxisVector(Matrix axes, Axis axis) => axis switch
        {
            Axis.X => new Vector3(axes.M11, axes.M12, axes.M13),

            Axis.Y => new Vector3(axes.M21, axes.M22, axes.M23),

            _ => new Vector3(axes.M31, axes.M32, axes.M33)
        };

        private static Matrix ViewAxes(Renderer.Renderer owner)
        {
            var forward = owner.ViewDirection;

            var cross = Vector3.Cross(Vector3.Up, forward);

            var right = cross.LengthSquared() < 1e-8f ? Vector3.Right : Vector3.Normalize(cross);

            var up = Vector3.Normalize(Vector3.Cross(forward, right));

            return new Matrix(
                right.X, right.Y, right.Z, 0f,
                up.X, up.Y, up.Z, 0f,
                forward.X, forward.Y, forward.Z, 0f,
                0f, 0f, 0f, 1f);
        }

        private static Matrix LocalAxes(TransformSet? targets)
        {
            if (targets is not TransformTargets vertices || vertices.Count == 0)
                return Matrix.Identity;

            return LocalOf(vertices.Order[0].Shape);
        }

        private static Matrix LocalOf(RenderShape shape)
        {
            var local = shape.InverseTransform;

            var world = Matrix.Identity;

            Matrix.Invert(ref local, out world);

            world.M14 = 0f;

            world.M24 = 0f;

            world.M34 = 0f;

            world.M41 = 0f;

            world.M42 = 0f;

            world.M43 = 0f;

            world.M44 = 1f;

            var x = Vector3.Normalize(new Vector3(world.M11, world.M12, world.M13));

            var y = Vector3.Normalize(new Vector3(world.M21, world.M22, world.M23));

            var z = Vector3.Normalize(new Vector3(world.M31, world.M32, world.M33));

            return new Matrix(
                x.X, x.Y, x.Z, 0f,
                y.X, y.Y, y.Z, 0f,
                z.X, z.Y, z.Z, 0f,
                0f, 0f, 0f, 1f);
        }
    }
}
