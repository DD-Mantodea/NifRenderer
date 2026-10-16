using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NifRenderer.Renderer.Vertex;
using NifRenderer.UI;
using NifRenderer.UI.Common;

namespace NifRenderer.Editing.Transform
{
    public sealed class TransformOperator
    {
        public static readonly TransformOperator Instance = new();

        private Renderer.Renderer? _owner;

        private Plane _plane;

        private Vector3 _start;

        private Vector3 _previous;

        private float _angle;

        private Matrix _axes = Matrix.Identity;

        private TransformConstraint _constraint = TransformConstraint.None;

        public bool Active => Targets != null;

        public bool HoldToDrag { get; private set; }

        public TransformMode? Mode { get; private set; }

        public TransformSet? Targets { get; private set; }

        public Orientation Orientation { get; private set; } = Orientation.Global;

        public Matrix Axes => _axes;

        public Vector3 Hit { get; private set; }

        public TransformConstraint Constraint => _constraint;

        public Vector3 Value { get; private set; }

        public string Status => Mode == null ? "" : _constraint.IsNone ? Mode.Name : $"{Mode.Name}  {_constraint.Text}";

        public bool Begin(Renderer.Renderer owner, IEnumerable<VertexRef> selected, TransformMode mode, int screenX, int screenY, bool holdToDrag = true) =>
            Begin(owner, TransformTargets.Capture(owner, selected), mode, screenX, screenY, holdToDrag);

        public bool Begin(Renderer.Renderer owner, TransformSet? targets, TransformMode mode, int screenX, int screenY, bool holdToDrag = true)
        {
            if (targets == null)
                return false;

            if (!owner.ScreenRay(screenX, screenY, out var origin, out var direction))
                return false;

            var normal = owner.ViewDirection;

            var plane = new Plane(normal, -Vector3.Dot(normal, targets.Pivot));

            if (!Renderer.Renderer.RayPlane(origin, direction, plane, out var start))
                return false;

            _owner = owner;

            _plane = plane;

            _start = start;

            _previous = start;

            _angle = 0f;

            Hit = start;

            _axes = Orientations.Axes(Orientation, owner, targets);

            _constraint = TransformConstraint.None;

            Mode = mode;

            Targets = targets;

            HoldToDrag = holdToDrag;

            Value = mode.Identity;

            SetCursorVisible(false);

            return true;
        }

        private static void SetCursorVisible(bool visible)
        {
            var host = GameHost.Instance;

            if (host != null)
                host.IsMouseVisible = visible;
        }

        public void Update(int screenX, int screenY)
        {
            if (_owner == null || Targets == null || Mode == null)
                return;

            if (!_owner.ScreenRay(screenX, screenY, out var origin, out var direction))
                return;

            if (!Renderer.Renderer.RayPlane(origin, direction, _plane, out var hit))
                return;

            Hit = hit;

            if (Mode.AngleInput)
            {
                _angle += AngleDelta(_previous - Targets.Pivot, hit - Targets.Pivot, _owner.ViewDirection);

                _previous = hit;

                Value = Mode.Value(new Vector3(_angle, 0f, 0f), hit - Targets.Pivot, _start - Targets.Pivot);
            }
            else
            {
                var offset = _constraint.Apply(hit - _start);

                Value = Mode.Value(offset, hit - Targets.Pivot, _start - Targets.Pivot);
            }

            Targets.Apply(_owner, Mode, Value);
        }

        private static float AngleDelta(Vector3 previous, Vector3 current, Vector3 viewDirection)
        {
            previous -= Vector3.Dot(previous, viewDirection) * viewDirection;

            current -= Vector3.Dot(current, viewDirection) * viewDirection;

            if (previous.LengthSquared() < 1e-8f || current.LengthSquared() < 1e-8f)
                return 0f;

            previous.Normalize();

            current.Normalize();

            var dot = Math.Clamp(Vector3.Dot(previous, current), -1f, 1f);

            var delta = MathF.Acos(dot);

            if (Vector3.Dot(Vector3.Cross(previous, current), viewDirection) < 0f)
                delta = -delta;

            return delta;
        }

        public void Cancel()
        {
            if (_owner == null || Targets == null)
                return;

            Targets.Restore(_owner);

            Clear();
        }

        public EditRecord? Confirm()
        {
            if (_owner == null || Targets == null || Mode == null)
                return null;

            var record = Mode.HasEffect(Value) ? Targets.BuildRecord(Mode, Value) : null;

            Clear();

            return record;
        }

        public bool HandleInput(InputEventArgs args, out EditRecord? record)
        {
            record = null;

            if (!Active)
                return false;

            switch (args.Type)
            {
                case InputType.MouseMove:
                    Update((int)args.MousePosition.X, (int)args.MousePosition.Y);

                    return true;

                case InputType.KeyDown:
                    return HandleKey(args.Key, args.Shift, out record);

                case InputType.MouseLeftDown:
                    if (HoldToDrag)
                        return false;

                    record = Confirm();

                    return true;

                case InputType.MouseRightDown:
                    if (HoldToDrag)
                        return false;

                    Cancel();

                    return true;

                case InputType.MouseLeftUp:
                    if (HoldToDrag)
                    {
                        record = Confirm();

                        return true;
                    }

                    return true;
            }

            return false;
        }

        public void SetConstraint(Axis axis, bool plane)
        {
            var same = plane
                ? _constraint.LockedPlane == axis
                : _constraint.LockedAxis == axis;

            if (same)
            {
                _constraint = TransformConstraint.None;

                SyncRotationAxis();

                return;
            }

            var repeat = plane
                ? _constraint.LockedPlane != null || _constraint.LockedAxis != null
                : _constraint.LockedAxis != null || _constraint.LockedPlane != null;

            if (repeat && _owner != null)
            {
                Orientation = Next(Orientation);

                _axes = Orientations.Axes(Orientation, _owner, Targets);
            }

            _constraint = plane
                ? TransformConstraint.OnPlane(axis, Orientation, _axes)
                : TransformConstraint.OnAxis(axis, Orientation, _axes);

            SyncRotationAxis();
        }

        private void SyncRotationAxis()
        {
            if (Targets == null || _owner == null)
                return;

            Targets.RotationAxis = _constraint.LockedAxis is { } locked
                ? Orientations.AxisVector(_axes, locked)
                : _constraint.LockedPlane is { } normal
                    ? Orientations.AxisVector(_axes, normal)
                    : _owner.ViewDirection;
        }

        public void Clear()
        {
            _owner = null;

            Targets = null;

            Mode = null;

            Value = Vector3.Zero;

            _constraint = TransformConstraint.None;

            HoldToDrag = true;

            SetCursorVisible(true);
        }

        private bool HandleKey(Keys key, bool shift, out EditRecord? record)
        {
            record = null;

            switch (key)
            {
                case Keys.Enter:
                    record = Confirm();

                    return true;

                case Keys.Escape:
                    Cancel();

                    return true;

                case Keys.G:
                    Mode = new TranslateMode();

                    return true;

                case Keys.S:
                    Mode = new ResizeMode();

                    return true;

                case Keys.R:
                    Mode = new RotateMode();

                    return true;

                case Keys.X:
                    SetConstraint(Axis.X, shift);

                    Refresh();

                    return true;

                case Keys.Y:
                    SetConstraint(Axis.Y, shift);

                    Refresh();

                    return true;

                case Keys.Z:
                    SetConstraint(Axis.Z, shift);

                    Refresh();

                    return true;
            }

            return false;
        }

        private void Refresh()
        {
            if (_owner == null || Targets == null || Mode == null)
                return;

            var mouse = UserInput.CurrentMouseState;

            Update(mouse.X, mouse.Y);

            _previous = Hit;
        }

        private static Orientation Next(Orientation orientation) => orientation switch
        {
            Orientation.Global => Orientation.Local,

            Orientation.Local => Orientation.View,

            _ => Orientation.Global
        };
    }
}
