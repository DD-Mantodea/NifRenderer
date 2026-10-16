using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace NifRenderer.UI.Components.Docking
{
    public static class DockHit
    {
        public static float CenterRatio = 0.4f;

        public static DockPosition? GetPosition(Rectangle rect, Vector2 mousePos)
        {
            if (!rect.Contains(mousePos))
                return null;

            var x = (mousePos.X - rect.Left) / Math.Max(1, rect.Width);

            var y = (mousePos.Y - rect.Top) / Math.Max(1, rect.Height);

            var edge = (1f - CenterRatio) / 2f;

            if (x < edge)
                return DockPosition.Left;

            if (x > 1f - edge)
                return DockPosition.Right;

            if (y < edge)
                return DockPosition.Top;

            if (y > 1f - edge)
                return DockPosition.Bottom;

            return DockPosition.Center;
        }
    }

    public enum DockPosition
    {
        Center,
        Left,
        Right,
        Top,
        Bottom
    }
}
