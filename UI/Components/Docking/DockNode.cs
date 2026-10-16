using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace NifRenderer.UI.Components.Docking
{
    public abstract class DockNode
    {
        public DockSplitNode? Parent;

        public Rectangle Rect;

        public abstract Rectangle ContentRect { get; }
    }
}
