using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NifRenderer.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NifRenderer.UI
{
    public class Component : IUserInterface
    {
        #region Visual
        public Color BackgroundColor;
        #endregion

        #region Data
        public int Width = 0;

        public int Height = 0;

        public float Alpha = 1;

        public Vector2 Position = new(0, 0);

        public bool CanHit = true;

        public bool IsHovering = false;

        public bool Visible = true;

        public bool Enabled = true;

        public bool Scissor = false;

        public string ID = "";

        public Vector2 Size => new(Width, Height);

        public virtual Rectangle Rectangle => new((int)Position.X, (int)Position.Y, Width, Height);
        #endregion

        #region Tree
        public Component? Parent;

        public List<Component> Children = [];
        #endregion

        public virtual void PreDraw(SpriteBatch spriteBatch, GameTime gameTime) { }

        public virtual void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!Visible)
                return;

            if (BackgroundColor != default)
                spriteBatch.DrawRectangle(new((int)Position.X, (int)Position.Y, Width, Height), BackgroundColor * Alpha);

            if (!Scissor)
            {
                DrawChildren(spriteBatch, gameTime);

                return;
            }

            spriteBatch.End();

            UIManager.Instance.ScissorStack.Push(Rectangle);

            spriteBatch.Begin(rasterizerState: UIManager.ScissorRasterizer);

            try
            {
                DrawChildren(spriteBatch, gameTime);
            }
            finally
            {
                UIManager.Instance.ScissorStack.Pop();

                spriteBatch.End();

                spriteBatch.Begin(rasterizerState: UIManager.ScissorRasterizer);
            }
        }

        public virtual void DrawChildren(SpriteBatch spriteBatch, GameTime gameTime)
        {
            foreach (var child in Children.ToArray())
                child.Draw(spriteBatch, gameTime);
        }

        public virtual void PostDraw(SpriteBatch spriteBatch, GameTime gameTime) { }

        public virtual void Unload() { }

        public virtual void Update(GameTime gameTime)
        {
            if (!Enabled)
                return;

            var mouseRect = UserInput.GetMouseRectangle();

            IsHovering = mouseRect.Intersects(Rectangle);
        }

        #region Interact
        public virtual Component? HitComponent(Vector2 position)
        {
            if (!Visible || !Enabled)
                return null;

            for (var i = Children.Count - 1; i >= 0; i--)
            {
                var result = Children[i].HitComponent(position);

                if (result != null)
                    return result;
            }

            if (CanHit && Rectangle.Contains(position))
                return this;

            return null;
        }

        public virtual InputResult OnInput(InputEventArgs args) => InputResult.Continue;
        #endregion

        #region Children
        public virtual void RegisterChild(Component component)
        {
            if (SelectChildById(component.ID) != null)
                return;

            Children.Add(component);

            component.Parent = this;
        }

        public virtual void UnregisterChild(Component component)
        {
            if (!Children.Remove(component))
                return;

            component.Parent = null;
        }

        public virtual void RegisterChildAt(int index, Component component, bool behind = false)
        {
            if (SelectChildById(component.ID) != null)
                return;
            if (behind)
                Children.Insert(Children.Count - index, component);
            else
                Children.Insert(index, component);

            component.Parent = this;
        }

        public bool ContainsChild(Predicate<Component> match)
        {
            return Children.Exists(match);
        }

        public Component SelectChild(Func<Component, bool> match)
        {
            return Children.FirstOrDefault(match, null);
        }

        public T SelectChild<T>(Func<Component, bool> match) where T : Component
        {
            if (Children.FirstOrDefault(match) is not T) return null;
            return Children.FirstOrDefault(match, null) as T;
        }

        public List<Component> SelectChildren(Func<Component, bool> match)
        {
            return Children.Where(match).ToList();
        }

        public T SelectChildById<T>(string id) where T : Component
        {
            if (id == "") return null;
            if (Children.FirstOrDefault(c => c.ID == id, null) is not T) return null;
            else return Children.FirstOrDefault(c => c.ID == id, null) as T;
        }

        public Component SelectChildById(string id)
        {
            if (id == "") return null;
            return Children.FirstOrDefault(c => c.ID == id, null);
        }

        public void RemoveChildAt(int index)
        {
            if (Children.Count <= index)
                return;

            Children[index].Parent = null;

            Children[index].Unload();

            Children.RemoveAt(index);
        }

        public void RemoveChild(Component component)
        {
            if (component is null || !Children.Remove(component))
                return;

            component.Parent = null;

            component.Unload();
        }

        public void RemoveAllChild()
        {
            foreach (var child in Children.ToArray())
            {
                child.Parent = null;

                child.Unload();
            }

            Children.Clear();
        }
        #endregion
    }

    public interface IUserInterface
    {
        void Draw(SpriteBatch spriteBatch, GameTime gameTime);
    }
}
