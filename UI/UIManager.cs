using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace NifRenderer.UI
{
    public sealed class UIManager(Component root)
    {
        public static UIManager? Instance;

        public ScissorStack ScissorStack = new(GameHost.Instance.GraphicsDevice);

        public Component Root { get; } = root;

        public Component? FocusedComponent { get; set; }

        public Component? CapturedComponent { get; set; }

        public static readonly RasterizerState ScissorRasterizer = new()
        {
            CullMode = CullMode.None,
            ScissorTestEnable = true
        };

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Begin(rasterizerState: ScissorRasterizer, depthStencilState: DepthStencilState.None, blendState: BlendState.AlphaBlend);

            Root.Draw(spriteBatch, gameTime);

            spriteBatch.End();
        }

        public void Update(GameTime gameTime)
        {
            Root.Update(gameTime);
        }

        private static int _clipDepth;

        public static void Clipped(SpriteBatch spriteBatch, Rectangle clip, Action draw)
        {
            var device = spriteBatch.GraphicsDevice;

            var saved = device.ScissorRectangle;

            var target = Rectangle.Intersect(saved, clip);

            if (_clipDepth > 0)
            {
                device.ScissorRectangle = target;

                try
                {
                    draw();
                }
                finally
                {
                    device.ScissorRectangle = saved;
                }

                return;
            }

            _clipDepth++;

            spriteBatch.End();

            device.ScissorRectangle = target;

            device.BlendState = BlendState.AlphaBlend;

            device.DepthStencilState = DepthStencilState.None;

            spriteBatch.Begin(rasterizerState: ScissorRasterizer);

            try
            {
                draw();
            }
            finally
            {
                spriteBatch.End();

                device.ScissorRectangle = saved;

                device.BlendState = BlendState.AlphaBlend;

                device.DepthStencilState = DepthStencilState.None;

                spriteBatch.Begin(rasterizerState: ScissorRasterizer);

                _clipDepth--;
            }
        }

        #region Dispatch
        public InputResult Dispatch(InputEventArgs args)
        {
            if (!UserInput.HasFocus)
                return InputResult.Continue;

            if (args.Type == InputType.MouseLeftDown && Components.TextBox.Active is { } box && !box.Rectangle.Contains(args.MousePosition.ToPoint()))
                box.Finish(true);

            var target = args.Type switch
            {
                InputType.KeyDown or InputType.KeyUp or InputType.TextInput => FocusedComponent ?? Root,

                _ => CapturedComponent ?? Root.HitComponent(args.MousePosition),
            };

            if (target == null)
                return InputResult.Continue;

            if (args.Type is InputType.MouseMove or InputType.MouseLeftDown or InputType.MouseWheel)
                FocusedComponent = target;

            for (var component = target; component != null; component = component.Parent)
            {
                var result = component.OnInput(args);

                if (result is InputResult.Consumed)
                    return InputResult.Consumed;
            }

            return InputResult.Continue;
        }

        public InputResult DispatchTextInput(char character) => Dispatch(new() { Type = InputType.TextInput, Character = character, Shift = UserInput.Shift, Ctrl = UserInput.Ctrl, Alt = UserInput.Alt });

        public InputResult DispatchMouseMove(Vector2 mousePos, Vector2 mouseDelta) => Dispatch(new() { Type = InputType.MouseMove, MousePosition = mousePos, MouseDelta = mouseDelta, Shift = UserInput.Shift, Ctrl = UserInput.Ctrl, Alt = UserInput.Alt });
        public InputResult DispatchMouseWheel(Vector2 mousePos, int wheelDelta) => Dispatch(new() { Type = InputType.MouseWheel, MousePosition = mousePos, WheelDelta = wheelDelta, Shift = UserInput.Shift, Ctrl = UserInput.Ctrl, Alt = UserInput.Alt });

        public InputResult DispatchMouseLeftDown(Vector2 mousePos) => Dispatch(new() { Type = InputType.MouseLeftDown, MousePosition = mousePos, MouseButtonState = ButtonState.Pressed, Shift = UserInput.Shift, Ctrl = UserInput.Ctrl, Alt = UserInput.Alt });

        public InputResult DispatchMouseLeftUp(Vector2 mousePos) => Dispatch(new() { Type = InputType.MouseLeftUp, MousePosition = mousePos, MouseButtonState = ButtonState.Released, Shift = UserInput.Shift, Ctrl = UserInput.Ctrl, Alt = UserInput.Alt });

        public InputResult DispatchMouseRightDown(Vector2 mousePos) => Dispatch(new() { Type = InputType.MouseRightDown, MousePosition = mousePos, MouseButtonState = ButtonState.Pressed, Shift = UserInput.Shift, Ctrl = UserInput.Ctrl, Alt = UserInput.Alt });

        public InputResult DispatchMouseRightUp(Vector2 mousePos) => Dispatch(new() { Type = InputType.MouseRightUp, MousePosition = mousePos, MouseButtonState = ButtonState.Released, Shift = UserInput.Shift, Ctrl = UserInput.Ctrl, Alt = UserInput.Alt });

        public InputResult DispatchKeyDown(Keys key) => Dispatch(new() { Type = InputType.KeyDown, Key = key, MousePosition = UserInput.GetMousePos(), Shift = UserInput.Shift, Ctrl = UserInput.Ctrl, Alt = UserInput.Alt });

        public InputResult DispatchKeyUp(Keys key) => Dispatch(new() { Type = InputType.KeyUp, Key = key, MousePosition = UserInput.GetMousePos(), Shift = UserInput.Shift, Ctrl = UserInput.Ctrl, Alt = UserInput.Alt });
        #endregion
    }
}
