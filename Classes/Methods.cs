using System;
using System.Net;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32;
using Utilities;
using System.Windows.Forms;
using Octokit;
using System.IO;
using System.Threading;
using System.ComponentModel;

namespace AlwaysOnTop.Classes
{

    class Methods
    {
        #region imports
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        [DllImport("user32.dll")]
        static extern bool SetWindowText(IntPtr hWnd, string text);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);
        #endregion

        #region consts
        const UInt32 SW_HIDE = 0;
        const UInt32 SW_SHOWNORMAL = 1;
        const UInt32 SW_NORMAL = 1;
        const UInt32 SW_SHOWMINIMIZED = 2;
        const UInt32 SW_SHOWMAXIMIZED = 3;
        const UInt32 SW_MAXIMIZE = 3;
        const UInt32 SW_SHOWNOACTIVATE = 4;
        const UInt32 SW_SHOW = 5;
        const UInt32 SW_MINIMIZE = 6;
        const UInt32 SW_SHOWMINNOACTIVE = 7;
        const UInt32 SW_SHOWNA = 8;
        const UInt32 SW_RESTORE = 9;
        const int SWP_NOMOVE = 0x0002;
        const int SWP_NOSIZE = 0x0001;
        const int SWP_SHOWWINDOW = 0x0040;
        const int SWP_NOACTIVATE = 0x0010;
        const int HWND_TOPMOST = -1;
        const int HWND_NOTOPMOST = -2;
        const long WS_EX_TOPMOST = 0x00000008L;
        public enum GWL
        {
            GWL_WNDPROC = (-4),
            GWL_HINSTANCE = (-6),
            GWL_HWNDPARENT = (-8),
            GWL_STYLE = (-16),
            GWL_EXSTYLE = (-20),
            GWL_USERDATA = (-21),
            GWL_ID = (-12)
        }
        #endregion

        // This static method is required because Win32 does not support
        // GetWindowLongPtr directly
        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return GetWindowLongPtr32(hWnd, nIndex);
        }

        public static String GetWindowTitle()
        {
            int i = 0;
            while (i < 1)
            {
                const int nChars = 256;
                StringBuilder Buff = new StringBuilder(nChars);
                IntPtr handle = GetForegroundWindow();

                if (GetWindowText(handle, Buff, nChars) > 0)
                {
                    return Buff.ToString();
                }
            }
            return null;
        }
        public static void AoT_toggle(string title)
        {
            const string suffix = " - AlwaysOnTop";

            IntPtr handle = GetForegroundWindow();
            if (handle != IntPtr.Zero)
            {
                bool wasTopMost = IsWindowTopmost(handle);
                if (wasTopMost)
                {
                    SetWindowPos(handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                    if (title.EndsWith(suffix))
                    {
                        var newTitle = title.Substring(title.Length - suffix.Length);
                        SetWindowText(handle, newTitle);
                    }
                }
                else
                {
                    SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                    var newTitle = title + suffix;
                    SetWindowText(handle, newTitle);
                }
            }
        }
        public static bool IsWindowTopmost(IntPtr handle)
        {
            var val = (long)GetWindowLongPtr(handle, (int)GWL.GWL_EXSTYLE) & WS_EX_TOPMOST;
            return val != 0;
        }

        public static void AoT_on(string title)
        {
            Process[] processes = Process.GetProcesses(".");
            foreach (var process in processes)
            {
                string mWinTitle = process.MainWindowTitle.ToString();
                if (mWinTitle == title)
                {
                    IntPtr handle = process.MainWindowHandle;
                    if (handle != IntPtr.Zero)
                    {
                        SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                        string newTitle = title + " - AlwaysOnTop";
                        SetWindowText(handle, newTitle);
                    }
                }
            }
        }

        public static void AoT_off(string title)
        {
            Process[] processes = Process.GetProcesses(".");
            foreach (var process in processes)
            {
                string mWinTitle = process.MainWindowTitle.ToString();
                if (mWinTitle == title)
                {
                    IntPtr handle = process.MainWindowHandle;
                    if (handle != IntPtr.Zero)
                    {
                        SetWindowPos(handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                        string newTitle = title.Substring(0, title.Length - 14);
                        SetWindowText(handle, newTitle);
                    }
                }
            }
        }

        public static string TryRegString(RegistryKey rk, string keyName, string value, bool overwrite)
        {
            string temp;

            try
            {
                temp = (string)rk.GetValue(keyName);
            }
            catch
            {
                rk.SetValue(keyName, value, RegistryValueKind.String);
                temp = (string)rk.GetValue(keyName);
            }
            if (overwrite == true)
            {
                if (temp != value)
                {
                    rk.SetValue(keyName, value, RegistryValueKind.String);
                    temp = (string)rk.GetValue(keyName);
                }
            }

            return temp;
        }

        public static int TryRegInt(RegistryKey rk, string keyName, int value, bool overwrite)
        {
            int temp;

            try
            {
                temp = (int)rk.GetValue(keyName);
            }
            catch
            {
                rk.SetValue(keyName, value, RegistryValueKind.DWord);
                temp = (int)rk.GetValue(keyName);
            }

            if (overwrite == true)
            {
                if (temp != value)
                {
                    rk.SetValue(keyName, value, RegistryValueKind.DWord);
                    temp = (int)rk.GetValue(keyName);
                }
            }

            return temp;
        }

        public static void GetReleases()
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("AlwaysOnTop-Updater"));
                var releases = client.Repository.Release.GetAll("jparnell8839", "AlwaysOnTop").Result;
                Release latest = releases[0];
                var assets = latest.Assets;


                if (latest.TagName != AlwaysOnTop.version)
                {
                    DialogResult downloadUpdate = MessageBox.Show("You have " + AlwaysOnTop.version + " installed." + Environment.NewLine
                        + "The latest release is " + latest.TagName + Environment.NewLine
                        + Environment.NewLine
                        + "Would you like to download the newest update?",
                        "Update Check",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (downloadUpdate == DialogResult.Yes)
                    {
                        FormUpdate update = new FormUpdate(latest);
                        update.ShowDialog();
                    }
                }
                else
                {
                    MessageBox.Show("You are up to date!", "Update Check", MessageBoxButtons.OK);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        /*public static void GetReleases(int updateFreq, DateTime lastCheck)
        {
            Thread thread = new Thread(() =>
            {
                long wut = new int();
                TimeSpan lol = DateTime.Now - lastCheck;
                wut = long.Parse(lol.ToString("yyyMMddHHmmss"));
                try
                {
                    var client = new GitHubClient(new ProductHeaderValue("AlwaysOnTop-Updater"));
                    var releases = client.Repository.Release.GetAll("jparnell8839", "AlwaysOnTop").Result;
                    Release latest = releases[0];
                    var assets = latest.Assets;


                    if (latest.TagName != AlwaysOnTop.version)
                    {
                        DialogResult downloadUpdate = MessageBox.Show("You have " + AlwaysOnTop.version + " installed." + Environment.NewLine
                            + "The latest release is " + latest.TagName + Environment.NewLine
                            + Environment.NewLine
                            + "Would you like to download the newest update?",
                            "Update Check",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (downloadUpdate == DialogResult.Yes)
                        {
                            //MessageBox.Show(latest);
                            FormUpdate update = new FormUpdate(latest);
                            update.ShowDialog();
                        }
                    }
                    else
                    {
                        MessageBox.Show("You are up to date!", "Update Check", MessageBoxButtons.OK);
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            });
        }

        public static void ThreadComplete(object sender, AsyncCompletedEventArgs e)
        {

        }*/
    }
}
