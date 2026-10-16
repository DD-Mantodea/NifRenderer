using Microsoft.Xna.Framework;
using System;

namespace NifRenderer.UI.Components.Docking
{
    public static class DockLayout
    {
        public static void Arrange(DockNode node, Rectangle rect)
        {
            node.Rect = rect;

            if (node is DockTabNode tab)
                tab.UpdateTabLayout();

            if (node is not DockSplitNode split)
                return;

            var thickness = DockSplitNode.SplitterThickness;

            if (split.Direction == DockSplitDirection.Horizontal)
            {
                var available = Math.Max(0, rect.Width - thickness);

                var firstWidth = (int)(available * split.Ratio);

                split.SplitRect = new(rect.X + firstWidth, rect.Y, thickness, rect.Height);

                Arrange(split.First, new(rect.X, rect.Y, firstWidth, rect.Height));

                Arrange(split.Second, new(rect.X + firstWidth + thickness, rect.Y, available - firstWidth, rect.Height));
            }
            else
            {
                var available = Math.Max(0, rect.Height - thickness);

                var firstHeight = (int)(available * split.Ratio);

                split.SplitRect = new(rect.X, rect.Y + firstHeight, rect.Width, thickness);

                Arrange(split.First, new(rect.X, rect.Y, rect.Width, firstHeight));

                Arrange(split.Second, new(rect.X, rect.Y + firstHeight + thickness, rect.Width, available - firstHeight));
            }
        }
    }
}
