using Microsoft.Xna.Framework;
using NifRenderer.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace NifRenderer.UI.Components.Docking
{
    public sealed class DockSplitNode : DockNode
    {
        private const int BaseSplitterThickness = 6;

        public static int SplitterThickness => Math.Max(1, BaseSplitterThickness);

        public const float MinRatio = 0.05f;

        public const float MaxRatio = 0.95f;

        public DockSplitDirection Direction;

        public float Ratio = 0.5f;

        public DockNode First;

        public DockNode Second;

        public Rectangle SplitRect;

        public override Rectangle ContentRect => Rect;

        public DockSplitNode(DockSplitDirection direction, DockNode first, DockNode second, float ratio = 0.5f)
        {
            Direction = direction;

            First = first;

            Second = second;

            Ratio = Math.Clamp(ratio, MinRatio, MaxRatio);

            First.Parent = this;

            Second.Parent = this;
        }

        public void SetChildren(DockNode first, DockNode second)
        {
            First.Parent = null;

            Second.Parent = null;

            First = first;

            Second = second;

            First.Parent = this;

            Second.Parent = this;
        }
    }

    public enum DockSplitDirection
    {
        Horizontal,
        Vertical
    }
}
