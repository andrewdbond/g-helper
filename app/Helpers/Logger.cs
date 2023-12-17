using System.Diagnostics;

public static class Logger
{
    public static string appPath = Environment.GetFolderPath(folder: Environment.SpecialFolder.ApplicationData) + "\\GHelper";
    public static string logFile = appPath + "\\log.txt";

    public static void WriteLine(string logMessage)
    {
        Debug.WriteLine(message: logMessage);
        if (!Directory.Exists(path: appPath)) Directory.CreateDirectory(path: appPath);

        try
        {
            using (StreamWriter w = File.AppendText(path: logFile))
            {
                w.WriteLine(value: $"{DateTime.Now}: {logMessage}");
                w.Close();
            }
        }
        catch { }

        if (new Random().Next(maxValue: 100) == 1) Cleanup();


    }

    public static void Cleanup()
    {
        try
        {
            var file = File.ReadAllLines(path: logFile);
            int skip = Math.Max(val1: 0, val2: file.Count() - 1000);
            File.WriteAllLines(path: logFile, contents: file.Skip(count: skip).ToArray());
        }
        catch { }
    }

}
