using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace NifRenderer.Extensions
{
    public static class MouseStateExt
    {
        extension(MouseState state)
        {
            public Vector2 Position() => new(state.X, state.Y);

            public bool MouseLeft() => state.LeftButton == ButtonState.Pressed;

            public bool MouseRight() => state.RightButton == ButtonState.Pressed;
        }
    }
}
