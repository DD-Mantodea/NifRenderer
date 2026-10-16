using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using NifRenderer.Utils;

namespace NifRenderer.UI.Dialogs
{
    public static class OpenDialog
    {
        private static bool _open;

        public static string? Ask(string title, string filter, string initialPath)
        {
            if (_open)
                return null;

            _open = true;

            try
            {

                if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                    return Run(title, filter, initialPath);

                string? chosen = null;

                var thread = new Thread(() => chosen = Run(title, filter, initialPath));

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

        private static string? Run(string title, string filter, string initialPath)
        {
            try
            {
                var directory = Path.GetDirectoryName(initialPath);

                var name = Path.GetFileName(initialPath);

                using var dialog = new OpenFileDialog
                {
                    FileName = string.IsNullOrEmpty(name) ? "" : name,

                    InitialDirectory = Directory.Exists(directory) ? directory : AppContext.BaseDirectory,

                    Filter = filter,

                    CheckFileExists = true,

                    RestoreDirectory = true,

                    Title = title
                };

                var result = dialog.ShowDialog();


                return result == DialogResult.OK ? dialog.FileName : null;
            }
            catch (Exception exception)
            {

                return null;
            }
        }
    }
}
