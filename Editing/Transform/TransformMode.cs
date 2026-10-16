using System;
using Microsoft.Xna.Framework;
using NifRenderer.Renderer.Vertex;

namespace NifRenderer.Editing.Transform
{
    public abstract class TransformMode
    {
        public abstract string Name { get; }

        public virtual bool SpringHelpline => false;

        public virtual bool AngleInput => false;

        public virtual float ArrowAngle => 0f;

        public abstract Vector3 Identity { get; }

        public abstract bool HasEffect(Vector3 value);

        public abstract Vector3 Value(Vector3 offset, Vector3 fromCenter, Vector3 startFromCenter);

        public abstract Vector3 Apply(Vector3 origin, Vector3 pivot, Vector3 axis, Vector3 value);
    }

    public sealed class TranslateMode : TransformMode
    {
        public override string Name => "Move";

        public override Vector3 Identity => Vector3.Zero;

        public override bool HasEffect(Vector3 value) => value.LengthSquared() > 1e-8f;

        public override Vector3 Value(Vector3 offset, Vector3 fromCenter, Vector3 startFromCenter) => offset;

        public override Vector3 Apply(Vector3 origin, Vector3 pivot, Vector3 axis, Vector3 value) => origin + value;
    }

    public sealed class ResizeMode : TransformMode
    {
        public override string Name => "Scale";

        public override bool SpringHelpline => true;

        public override Vector3 Identity => Vector3.One;

        public override bool HasEffect(Vector3 value) => MathF.Abs(value.X - 1f) > 1e-4f;

        public override Vector3 Value(Vector3 offset, Vector3 fromCenter, Vector3 startFromCenter)
        {
            var reference = startFromCenter.Length();

            if (reference < 1e-5f)
                reference = 1f;

            return Vector3.One * (fromCenter.Length() / reference);
        }

        public override Vector3 Apply(Vector3 origin, Vector3 pivot, Vector3 axis, Vector3 value) =>
            pivot + (origin - pivot) * value.X;
    }

    public sealed class RotateMode : TransformMode
    {
        public override string Name => "Rotate";

        public override bool AngleInput => true;

        public override bool SpringHelpline => true;

        public override float ArrowAngle => MathF.PI / 2f;

        public override Vector3 Identity => Vector3.Zero;

        public override bool HasEffect(Vector3 value) => MathF.Abs(value.X) > 1e-5f;

        public override Vector3 Value(Vector3 offset, Vector3 fromCenter, Vector3 startFromCenter) => Vector3.One * offset.X;

        public override Vector3 Apply(Vector3 origin, Vector3 pivot, Vector3 axis, Vector3 value) =>
            pivot + Vector3.Transform(origin - pivot, Matrix.CreateFromAxisAngle(axis, value.X));
    }
}
