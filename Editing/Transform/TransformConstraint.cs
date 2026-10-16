using Microsoft.Xna.Framework;

namespace NifRenderer.Editing.Transform
{
    public sealed class TransformConstraint
    {
        public static readonly TransformConstraint None = new("", Matrix.Identity, null, null);

        private TransformConstraint(string text, Matrix projection, Axis? axis, Axis? plane)
        {
            Text = text;

            Projection = projection;

            LockedAxis = axis;

            LockedPlane = plane;
        }

        public string Text { get; }

        public Matrix Projection { get; }

        public Axis? LockedAxis { get; }

        public Axis? LockedPlane { get; }

        public bool IsNone => LockedAxis == null && LockedPlane == null;

        public Vector3 Apply(Vector3 value) => IsNone ? value : Vector3.Transform(value, Projection);

        public static TransformConstraint OnAxis(Axis axis, Orientation orientation, Matrix axes) =>
            new($"{orientation} {axis}", Project(keep: axis, zero: null, axes), axis, null);

        public static TransformConstraint OnPlane(Axis normal, Orientation orientation, Matrix axes) =>
            new($"{orientation} {normal} plane", Project(keep: null, zero: normal, axes), null, normal);

        private static Matrix Project(Axis? keep, Axis? zero, Matrix axes)
        {
            var diagonal = Matrix.Identity;

            if (keep != null)
            {
                diagonal.M11 = keep == Axis.X ? 1f : 0f;

                diagonal.M22 = keep == Axis.Y ? 1f : 0f;

                diagonal.M33 = keep == Axis.Z ? 1f : 0f;
            }
            else
            {
                diagonal.M11 = zero == Axis.X ? 0f : 1f;

                diagonal.M22 = zero == Axis.Y ? 0f : 1f;

                diagonal.M33 = zero == Axis.Z ? 0f : 1f;
            }

            var transpose = Matrix.Transpose(axes);

            return transpose * diagonal * axes;
        }
    }
}
