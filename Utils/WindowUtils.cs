using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NifRenderer.Utils
{
    public static class WindowUtils
    {
        private const int DwmwaUseImmersiveDarkMode = 20;

        private const int DwmwaBorderColor = 34;

        private const int DwmwaCaptionColor = 35;

        private const int DwmwaTextColor = 36;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hwnd, int command);

        private const int HideWindowCommand = 0;

        private const int ShowWindowCommand = 5;

        public static void Hide() => Apply(HideWindowCommand);

        public static void Show() => Apply(ShowWindowCommand);

        private static void Apply(int command)
        {
            var handle = ResolveHandle();

            if (handle != IntPtr.Zero)
                ShowWindow(handle, command);
        }

        public static bool HasFocus
        {
            get
            {
                var handle = ResolveHandle();

                return handle == IntPtr.Zero || GetForegroundWindow() == handle;
            }
        }

        public static bool ApplyTitleBarColor(Color background, Color text)
        {
            var handle = ResolveHandle();

            if (handle == IntPtr.Zero)
                return false;

            var dark = 1;

            var ok = TrySet(handle, DwmwaUseImmersiveDarkMode, dark);

            var caption = ToColorRef(background);

            ok |= TrySet(handle, DwmwaCaptionColor, caption);

            ok |= TrySet(handle, DwmwaBorderColor, caption);

            var textColor = ToColorRef(text);

            ok |= TrySet(handle, DwmwaTextColor, textColor);

            return ok;
        }

        public static IntPtr Handle => ResolveHandle();

        private static IntPtr ResolveHandle()
        {
            using var process = Process.GetCurrentProcess();

            return process.MainWindowHandle;
        }

        private static bool TrySet(IntPtr handle, int attribute, int value)
        {
            try
            {
                return DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int)) == 0;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }

        private static int ToColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);
    }
}
