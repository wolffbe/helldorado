using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// HelldoradoFixed.exe - launches Helldorado borderless-fullscreen on modern Windows.
//
// Helldorado (2007, Trinigy Vision Engine) fails to start on Windows 10/11 with
// "Unable to initialize video mode / Could not set screen mode" because it tries to take
// exclusive fullscreen at a mode modern GPUs reject. This launcher forces windowed mode at
// the real screen resolution, starts the game, then strips the window border and stretches
// it to fill the screen - so it looks fullscreen but never does the failing mode-switch.
// The original Helldorado.exe is not modified.
class HelldoradoFixed
{
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern int  GetSystemMetrics(int i);
    [DllImport("user32.dll")] static extern int  GetWindowLong(IntPtr h, int i);
    [DllImport("user32.dll")] static extern int  SetWindowLong(IntPtr h, int i, int v);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);

    const int GWL_STYLE = -16, WS_CAPTION = 0x00C00000, WS_THICKFRAME = 0x00040000;

    [STAThread]
    static void Main()
    {
        SetProcessDPIAware();
        int w = GetSystemMetrics(0), h = GetSystemMetrics(1);

        // find the game: this exe's own folder first, then common GOG / Steam installs
        string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
        string[] candidates = {
            baseDir,
            @"C:\Program Files (x86)\GOG Galaxy\Games\Helldorado",
            @"C:\Program Files (x86)\Steam\steamapps\common\Helldorado"
        };
        string gameDir = null, gameExe = null;
        foreach (string c in candidates)
        {
            string e = Path.Combine(c, "Helldorado.exe");
            if (File.Exists(e)) { gameDir = c; gameExe = e; break; }
        }
        if (gameExe == null)
        {
            MessageBox.Show("Helldorado.exe not found.\nPut HelldoradoFixed.exe in your Helldorado game folder and run it again.",
                "Helldorado Fixed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // make sure the display config is windowed at the detected desktop resolution
        try
        {
            string settings = Path.Combine(gameDir, @"data\configuration\game\settings.xml");
            if (File.Exists(settings))
            {
                string t = File.ReadAllText(settings);
                t = Regex.Replace(t, "(name=\"ENGINE_FULLSCREEN\"\\s+value=\")\\d+(\")",   "${1}0${2}");
                t = Regex.Replace(t, "(name=\"ENGINE_RESOLUTION_X\"\\s+value=\")\\d+(\")", "${1}" + w + "${2}");
                t = Regex.Replace(t, "(name=\"ENGINE_RESOLUTION_Y\"\\s+value=\")\\d+(\")", "${1}" + h + "${2}");
                File.WriteAllText(settings, t);
            }
        }
        catch { }

        // forward any command-line args (e.g. from GOG Galaxy)
        string[] argv = Environment.GetCommandLineArgs();
        var sb = new StringBuilder();
        for (int i = 1; i < argv.Length; i++) sb.Append("\"" + argv[i] + "\" ");

        Process p = Process.Start(new ProcessStartInfo(gameExe, sb.ToString().Trim()) { WorkingDirectory = gameDir, UseShellExecute = true });
        if (p == null) return;

        // strip border + fill screen (re-applied for a few seconds so it sticks past the intro)
        bool focused = false;
        for (int i = 0; i < 30 && !p.HasExited; i++)
        {
            Thread.Sleep(500);
            p.Refresh();
            IntPtr hwnd = p.MainWindowHandle;
            if (hwnd != IntPtr.Zero)
            {
                int style = GetWindowLong(hwnd, GWL_STYLE);
                int ns = style & ~(WS_CAPTION | WS_THICKFRAME);
                if (ns != style) SetWindowLong(hwnd, GWL_STYLE, ns);
                SetWindowPos(hwnd, IntPtr.Zero, 0, 0, w, h, 0x64); // FRAMECHANGED|SHOWWINDOW|NOZORDER
                if (!focused) { SetForegroundWindow(hwnd); focused = true; }
            }
        }
    }
}
