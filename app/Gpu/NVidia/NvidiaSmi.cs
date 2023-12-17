using Ryzen;
using System.Diagnostics;
using System.Text.RegularExpressions;

public static class NvidiaSmi
{
    public static bool GetDisplayActiveStatus()
    {
        // Non AMD devices doesn't seem to be affected
        if (!RyzenControl.IsAMD()) return false;

        string commandOutput = RunNvidiaSmiCommand();

        Logger.WriteLine(logMessage: commandOutput);

        if (commandOutput.Length == 0) return false;
        if (!commandOutput.Contains(value: "RTX 40")) return false;

        // Extract the "Display Active" status using regular expressions
        string displayActivePattern = @"Display Active\s+:\s+(\w+)";

        Match match = Regex.Match(input: commandOutput, pattern: displayActivePattern, options: RegexOptions.IgnoreCase);

        if (match.Success)
        {
            string status = match.Groups[groupnum: 1].Value.ToLower().Trim(trimChar: ' ');
            return status == "enabled";
        }

        return false; // Return false if the "Display Active" status is not found
    }

    private static string RunNvidiaSmiCommand(string arguments = "-i 0 -q")
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "nvidia-smi",
            Arguments = arguments,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = new Process
        {
            StartInfo = startInfo
        };

        try
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
        catch (Exception ex)
        {
            //return File.ReadAllText(@"smi.txt");
            Debug.WriteLine(message: ex.Message);
        }

        return "";

    }
}
