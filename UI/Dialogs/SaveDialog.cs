using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using NifRenderer.Utils;

namespace NifRenderer.UI.Dialogs
{
    public static class SaveDialog
    {
        private static bool _open;

        public static string? Ask(string initialPath)
        {
            if (_open)
                return null;

            _open = true;

            try
            {

                if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
                    return Run(initialPath);

                string? chosen = null;

                var thread = new Thread(() => chosen = Run(initialPath));

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

        private static string? Run(string initialPath)
        {
            try
            {
                var directory = Path.GetDirectoryName(initialPath);

                var name = Path.GetFileName(initialPath);

                using var dialog = new SaveFileDialog
                {
                    FileName = string.IsNullOrEmpty(name) ? "model_edited.nif" : name,

                    InitialDirectory = string.IsNullOrEmpty(directory) ? AppContext.BaseDirectory : directory,

                    Filter = "NIF 模型 (*.nif)|*.nif|所有文件 (*.*)|*.*",

                    DefaultExt = "nif",

                    AddExtension = true,

                    OverwritePrompt = true,

                    RestoreDirectory = true,

                    Title = "导出 NIF"
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
