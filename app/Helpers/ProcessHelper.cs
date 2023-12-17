using System.Diagnostics;
using System.Security.Principal;

namespace GHelper.Helpers
{
    public static class ProcessHelper
    {
        private static long lastAdmin;

        public static void CheckAlreadyRunning()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(processName: currentProcess.ProcessName);

            if (processes.Length > 1)
            {
                foreach (Process process in processes)
                    if (process.Id != currentProcess.Id)
                        try
                        {
                            process.Kill();
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLine(logMessage: ex.ToString());
                            MessageBox.Show(text: Properties.Strings.AppAlreadyRunningText, caption: Properties.Strings.AppAlreadyRunning, buttons: MessageBoxButtons.OK);
                            Application.Exit();
                            return;
                        }
            }
        }

        public static bool IsUserAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(ntIdentity: identity);
            return principal.IsInRole(role: WindowsBuiltInRole.Administrator);
        }

        public static void RunAsAdmin(string? param = null)
        {

            if (Math.Abs(value: DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastAdmin) < 2000) return;
            lastAdmin = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            // Check if the current user is an administrator
            if (!IsUserAdministrator())
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = Application.ExecutablePath;
                startInfo.Arguments = param;
                startInfo.Verb = "runas";
                try
                {
                    Process.Start(startInfo: startInfo);
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    Logger.WriteLine(logMessage: ex.Message);
                }
            }
        }


        public static void KillByName(string name)
        {
            foreach (var process in Process.GetProcessesByName(processName: name))
            {
                try
                {
                    process.Kill();
                    Logger.WriteLine(logMessage: $"Stopped: {process.ProcessName}");
                }
                catch (Exception ex)
                {
                    Logger.WriteLine(logMessage: $"Failed to stop: {process.ProcessName} {ex.Message}");
                }
            }
        }

        public static void KillByProcess(Process process)
        {
            try
            {
                process.Kill();
                Logger.WriteLine(logMessage: $"Stopped: {process.ProcessName}");
            }
            catch (Exception ex)
            {
                Logger.WriteLine(logMessage: $"Failed to stop: {process.ProcessName} {ex.Message}");
            }
        }

        public static void StopDisableService(string serviceName)
        {
            try
            {
                string script = $"Get-Service -Name \"{serviceName}\" | Stop-Service -Force -PassThru | Set-Service -StartupType Disabled";
                Logger.WriteLine(logMessage: script);
                RunCMD(name: "powershell", args: script);
            }
            catch (Exception ex)
            {
                Logger.WriteLine(logMessage: ex.ToString());
            }
        }

        public static void StartEnableService(string serviceName)
        {
            try
            {
                string script = $"Set-Service -Name \"{serviceName}\" -Status running -StartupType Automatic";
                Logger.WriteLine(logMessage: script);
                RunCMD(name: "powershell", args: script);
            }
            catch (Exception ex)
            {
                Logger.WriteLine(logMessage: ex.ToString());
            }
        }

        public static void RunCMD(string name, string args)
        {
            var cmd = new Process();
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            cmd.StartInfo.FileName = name;
            cmd.StartInfo.Arguments = args;
            cmd.Start();

            Logger.WriteLine(logMessage: args);

            string result = cmd.StandardOutput.ReadToEnd().Replace(oldValue: Environment.NewLine, newValue: " ").Trim(trimChar: ' ');

            Logger.WriteLine(logMessage: result);

            cmd.WaitForExit();
        }


    }
}
