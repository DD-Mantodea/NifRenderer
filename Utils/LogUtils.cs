using System;
using System.IO;
using System.Text;
using System.Threading;

namespace NifRenderer.Utils
{
    public static class LogUtils
    {
        private static readonly Lock Gate = new();

        private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "NifRenderer.log");

        public static bool Enabled { get; set; } =
#if DEBUG
            true;
#else
            false;
#endif

        public static void Write(string message)
        {
            if (!Enabled)
                return;

            var line = $"{DateTime.Now:HH:mm:ss.fff}  {message}{Environment.NewLine}";

            try
            {
                lock (Gate)
                    File.AppendAllText(LogPath, line);
            }
            catch (Exception)
            {
            }
        }

        public static void Write(Exception exception, string context)
        {
            var text = new StringBuilder();

            text.Append(context);

            text.Append(" -> ");

            text.Append(exception.GetType().Name);

            text.Append(": ");

            text.Append(exception.Message);

            text.Append("   @ ");

            text.Append(exception.StackTrace?.Split('\n') is { Length: > 0 } lines ? lines[0].Trim() : "?");

            Write(text.ToString());
        }

        public static void Crash(Exception? exception, string context)
        {
            if (exception == null)
                return;

            var text = new StringBuilder();

            text.Append(Environment.NewLine);

            text.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            text.Append("  ===== 崩溃 ");

            text.Append(context);

            text.Append(" =====");

            text.Append(Environment.NewLine);

            var depth = 0;

            for (var current = exception; current != null && depth < 8; current = current.InnerException, depth++)
            {
                text.Append(depth == 0 ? "  " : "  Inner: ");

                text.Append(current.GetType().FullName);

                text.Append(": ");

                text.Append(current.Message);

                text.Append(Environment.NewLine);

                text.Append(current.StackTrace);

                text.Append(Environment.NewLine);
            }

            text.Append("  ===== 崩溃结束 =====");

            text.Append(Environment.NewLine);

            try
            {
                lock (Gate)
                    File.AppendAllText(LogPath, text.ToString());
            }
            catch (Exception)
            {
            }
        }
    }
}
