using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NifRenderer.Extensions;
using NifRenderer.UI;
using System;
using System.Collections.Generic;

namespace NifRenderer.UI.Components.Docking
{
    public class DockTabNode : DockNode
    {
        private const int BaseTabBarHeight = 26;

        public static int TabBarHeight => UIScale.Value(BaseTabBarHeight);

        public const int MaxTabWidth = 160;

        public const int MinTabWidth = 48;

        public List<DockPanel> Panels = [];

        public DockPanel? ActivePanel;

        public List<Rectangle> TabRects = [];

        public int HoverTab { get; set; } = -1;

        public static DockTabNode? Focused;

        public override Rectangle ContentRect =>
            Panels.Count <= 1 && ActivePanel is { OwnsHeader: true }
                ? Rect
                : new Rectangle(Rect.X, Rect.Y + TabBarHeight, Rect.Width, Math.Max(0, Rect.Height - TabBarHeight));

        public void AddPanel(DockPanel panel)
        {
            if (Panels.Contains(panel))
            {
                panel.TabNode = this;

                return;
            }

            panel.TabNode = this;

            ScreenContainer.Instance?.RegisterChild(panel);

            Panels.Add(panel);

            ActivePanel ??= panel;
        }

        public bool RemovePanel(DockPanel panel)
        {
            if (!Panels.Remove(panel))
                return false;

            ScreenContainer.Instance.UnregisterChild(panel);

            if (ActivePanel == panel)
                ActivePanel = Panels.Count > 0 ? Panels[0] : null;

            return true;
        }

        public void SelectPanel(DockPanel panel)
        {
            if (Panels.Contains(panel))
                ActivePanel = panel;
        }

        public int HitTab(Point position)
        {
            for (var i = 0; i < TabRects.Count; i++)
            {
                if (TabRects[i].Contains(position))
                    return i;
            }

            return -1;
        }

        public bool IsOverTabBar(Point position) => Rect.Contains(position) && position.Y < Rect.Y + TabBarHeight;

        public void UpdateTabLayout()
        {
            TabRects.Clear();

            if (Panels.Count == 0 || Rect.Width <= 0)
                return;

            var width = Math.Clamp(Rect.Width / Panels.Count, MinTabWidth, MaxTabWidth);

            for (var i = 0; i < Panels.Count; i++)
            {
                var tabWidth = Math.Min(width, Math.Max(0, Rect.Right - (Rect.X + i * width)));

                if (tabWidth <= 0)
                    break;

                TabRects.Add(new Rectangle(Rect.X + i * width, Rect.Y, tabWidth, TabBarHeight));
            }
        }

        public void DrawActivePanel(SpriteBatch spriteBatch, GameTime gameTime)
        {
            ActivePanel?.Draw(spriteBatch, gameTime);
        }
    }

    public abstract class DockPanel(DockTabNode node) : Component
    {
        public virtual string Title => GetType().Name;

        public virtual bool HandlesHeaderInput(Point position) => false;

        public virtual bool OwnsHeader => false;

        public DockTabNode TabNode = node;
    }
}
