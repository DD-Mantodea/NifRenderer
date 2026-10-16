using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using NifRenderer.Utils;

namespace NifRenderer.UI.Dialogs
{
    public static class ProjectDialog
    {
        private static bool _open;

        [DllImport("user32.dll")]
        private static extern bool EnableWindow(IntPtr handle, bool enable);

        public static string? AskSave(string? current)
        {
            var initial = string.IsNullOrEmpty(current)
                ? Path.Combine(AppContext.BaseDirectory, "project.nifproj")
                : current;

            return Ask(initial, true);
        }

        public static string? AskOpen(string? current)
        {
            var initial = string.IsNullOrEmpty(current)
                ? Path.Combine(AppContext.BaseDirectory, "project.nifproj")
                : current;

            return Ask(initial, false);
        }

        private static string? Ask(string initial, bool save)
        {
            if (_open)
                return null;

            _open = true;

            try
            {
                if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                    return Run(initial, save);

                string? chosen = null;

                var thread = new Thread(() => chosen = Run(initial, save));

                thread.SetApartmentState(ApartmentState.STA);

                thread.IsBackground = true;

                thread.Start();

                thread.Join();

                return chosen;
            }
            finally
            {
                _open = false;
            }
        }

        private static string? Run(string initial, bool save)
        {
            var gameWindow = WindowUtils.Handle;

            if (gameWindow != IntPtr.Zero)
                EnableWindow(gameWindow, false);

            try
            {
                FileDialog dialog;

                if (save)
                {
                    dialog = new SaveFileDialog { OverwritePrompt = true, Title = "保存项目" };
                }
                else
                {
                    dialog = new OpenFileDialog { Title = "打开项目", CheckFileExists = true };
                }

                using (dialog)
                {
                    dialog.FileName = Path.GetFileName(initial);

                    dialog.InitialDirectory = Path.GetDirectoryName(initial) ?? AppContext.BaseDirectory;

                    dialog.Filter = "NifRenderer 项目 (*.nifproj)|*.nifproj|所有文件 (*.*)|*.*";

                    dialog.DefaultExt = "nifproj";

                    dialog.AddExtension = true;

                    dialog.RestoreDirectory = true;

                    return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
                }
            }
            catch (Exception exception)
            {

                return null;
            }
            finally
            {
                if (gameWindow != IntPtr.Zero)
                    EnableWindow(gameWindow, true);
            }
        }
    }
}
