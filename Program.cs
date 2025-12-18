using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TVCast
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AntdUI.Localization.DefaultLanguage = "zh-CN";
            AntdUI.Config.Theme().Dark("#000", "#fff").Light("#fff", "#000");
            AntdUI.Config.TextRenderingHighQuality = true;
            AntdUI.Config.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            AntdUI.Config.SetEmptyImageSvg(Properties.Resources.icon_empty, Properties.Resources.icon_empty_dark);
            AntdUI.SvgDb.Emoji = AntdUI.FluentFlat.Emoji;

            // 获取当前 EXE 所在目录
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string ffmpegPath = Path.Combine(currentDir, "ffmpeg.exe");

            // 如果 ffmpeg.exe 不存在，则从资源中提取
            if (!File.Exists(ffmpegPath))
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TVCast.ffmpeg.exe"))
                using (var file = File.Create(ffmpegPath))
                {
                    stream.CopyTo(file);
                }
            }
            Application.Run(new frmMain());
        }

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(ProcessDpiAwareness value);

        private enum ProcessDpiAwareness
        {
            ProcessDpiUnaware = 0,
            ProcessSystemDpiAware = 1,
            ProcessPerMonitorDpiAware = 2
        }
    }
}
