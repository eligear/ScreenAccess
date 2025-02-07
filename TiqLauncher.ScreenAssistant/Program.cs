﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace TiqLauncher.ScreenAssistant
{
    class Program
    {
        private const string ExeExt = ".exe";
        private const string SecretSubFolder = "bin";
        private static readonly string[] ExcludedFolders = {"Config", SecretSubFolder };
        private const string DefaultName = "backgroundTaskHost.exe";
        private static readonly Random Random = new Random();
        private static string CurrentDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? throw new InvalidOperationException();


        static void Main(string[] args)
        {
            for (var i = 0; i != args.Length; ++i)
            {
                if (args[i] == "/restart")
                {
                    Thread.Sleep(2000);
                }
            }
            PrepareBinaries();
//#if !DEBUG
            var lastName = Properties.Settings.Default.ScreenAssistantName;
            if (!RunSecretly(lastName)) RunSecretly(DefaultName);
//#else
//            RunSecretly(DefaultName);
//#endif
#if DEBUG
            Console.ReadKey();
#endif
        }

        private static void CreateSecureBinFolder(string folderPath)
        {
            // Way safer than string comparison against "BUILTIN\\Administrators"
            IdentityReference secureSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            //IdentityReference secureSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            DirectorySecurity dirSec = new DirectorySecurity();
            dirSec.AddAccessRule(new FileSystemAccessRule(secureSid, FileSystemRights.FullControl, AccessControlType.Allow));
            var directoryInfo = Directory.CreateDirectory(folderPath, dirSec);
            directoryInfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            // Grab ACL from folder
            var dirAccessControl = Directory.GetAccessControl(folderPath);
            dirAccessControl.SetOwner(secureSid);
        }

        private static void PrepareBinaries()
        {
            var launcherAssemblyName = Assembly.GetEntryAssembly().GetName().Name;
            CreateSecureBinFolder(Path.Combine(CurrentDirectory, SecretSubFolder));
            foreach (var file in Directory.GetFiles(CurrentDirectory).Where(x => !Path.GetFileName(x).StartsWith(launcherAssemblyName)))
            {
                var fileName = Path.GetFileName(file);
                var fileDirectory = Path.GetDirectoryName(file);
                var targetName = Path.Combine(fileDirectory, SecretSubFolder, fileName);
                if (File.Exists(targetName))
                {
                    File.Delete(targetName);
                }
                File.Move(file, targetName);
            }
            foreach (var file in Directory.GetDirectories(CurrentDirectory).Where(x => !Path.GetFileName(x).StartsWith(launcherAssemblyName) && !ExcludedFolders.Any(k => k.Equals(Path.GetFileName(x), StringComparison.OrdinalIgnoreCase))))
            {
                var fileName = Path.GetFileName(file);
                var fileDirectory = Path.GetDirectoryName(file);
                var targetName = Path.Combine(fileDirectory, SecretSubFolder, fileName);
                if (Directory.Exists(targetName))
                {
                    Directory.Delete(targetName, true);
                }
                Directory.Move(file, targetName);
            }
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowText(IntPtr hWnd, string text);

        private static bool RunSecretly(string name)
        {
            var currentPath = Path.Combine(CurrentDirectory, SecretSubFolder, name);
            var targetExeFile = new FileInfo(currentPath);
            if (targetExeFile.Exists)
            {
                StringSwapper.SwapFuzzyStrings(currentPath);
                var newName = RandomString(12) + ExeExt;
                var newPath = Path.Combine(Path.GetDirectoryName(currentPath) ?? throw new InvalidOperationException(), newName);
                targetExeFile.MoveTo(newPath);
                Properties.Settings.Default.ScreenAssistantName = newName;
                Properties.Settings.Default.Save();

                var startInfo = new ProcessStartInfo(targetExeFile.FullName);
                startInfo.UseShellExecute = false;
                startInfo.Arguments = "/launcher";
                var p = new Process { StartInfo = startInfo };
                p.Start();
                while (!p.WaitForInputIdle(0))
                {
                    Thread.Sleep(1500);
                }

                if (!p.HasExited)
                {
                    p.WaitForInputIdle();
                    SetWindowText(p.MainWindowHandle, newName);
                }

                return true;
            }
            return false;
        }

        internal static string RandomString(int length, string chars = "abcdefghijklmnopqrstuvwxyz0123456789")
        {
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }
    }
}
