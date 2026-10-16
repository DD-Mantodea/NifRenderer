using Microsoft.Xna.Framework;
using System;

namespace NifRenderer.UI.Components.Docking
{
    public static class DockTree
    {
        public static DockNode? FindRoot(DockNode node)
        {
            var current = node;

            while (current.Parent != null)
                current = current.Parent;

            return current;
        }

        public static DockTabNode? FindTab(DockNode? root, DockPanel panel)
        {
            switch (root)
            {
                case null:
                    return null;

                case DockTabNode tab:
                    return tab.Panels.Contains(panel) ? tab : null;

                case DockSplitNode split:
                    return FindTab(split.First, panel) ?? FindTab(split.Second, panel);

                default:
                    return null;
            }
        }

        public static DockNode? HitNode(DockNode? node, Point position)
        {
            if (node == null || !node.Rect.Contains(position))
                return null;

            if (node is DockSplitNode split)
                return HitNode(split.First, position) ?? HitNode(split.Second, position);

            return node;
        }

        public static bool Replace(ref DockNode root, DockNode target, DockNode replacement)
        {
            if (ReferenceEquals(root, target))
            {
                replacement.Parent = null;

                root = replacement;

                return true;
            }

            if (root is not DockSplitNode split)
                return false;

            if (ReferenceEquals(split.First, target))
            {
                split.First = replacement;

                replacement.Parent = split;

                return true;
            }

            if (ReferenceEquals(split.Second, target))
            {
                split.Second = replacement;

                replacement.Parent = split;

                return true;
            }

            return Replace(ref split.First, target, replacement) || Replace(ref split.Second, target, replacement);
        }

        public static void Collapse(ref DockNode root, DockNode node)
        {
            if (node is not DockTabNode tab || tab.Panels.Count > 0)
                return;

            if (root == node)
                return;

            var parent = tab.Parent;

            if (parent == null)
                return;

            var sibling = parent.First == tab ? parent.Second : parent.First;

            sibling.Parent = parent.Parent;

            var grand = parent.Parent;

            if (grand == null)
            {
                root = sibling;

                return;
            }

            if (grand.First == parent)
                grand.First = sibling;
            else
                grand.Second = sibling;
        }


        public static void InsertPanel(ref DockNode root, DockNode target, DockPosition position, DockPanel panel)
        {
            if (position == DockPosition.Center)
                position = DockPosition.Bottom;

            var direction = position is DockPosition.Left or DockPosition.Right
                ? DockSplitDirection.Horizontal
                : DockSplitDirection.Vertical;

            var newTab = new DockTabNode();

            newTab.AddPanel(panel);

            var newFirst = position is DockPosition.Left or DockPosition.Top;

            var split = newFirst
                ? new DockSplitNode(direction, newTab, target)
                : new DockSplitNode(direction, target, newTab);

            ReplaceAndCollapse(ref root, target, split);
        }

        public static void ReplaceAndCollapse(ref DockNode root, DockNode target, DockNode replacement)
        {
            if (!Replace(ref root, target, replacement))
                return;

            if (target is DockTabNode { Panels.Count: 0 } empty && root != empty)
                Collapse(ref root, empty);
        }
    }
}
