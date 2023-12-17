using GHelper.Helpers;
using Microsoft.Win32.TaskScheduler;
using System.Diagnostics;
using System.Security.Principal;

public class Startup
{

    static string taskName = "GHelper";

    public static bool IsScheduled()
    {
        using (TaskService taskService = new TaskService())
            return (taskService.RootFolder.AllTasks.Any(predicate: t => t.Name == taskName));
    }

    public static void ReScheduleAdmin()
    {
        if (ProcessHelper.IsUserAdministrator() && IsScheduled())
        {
            UnSchedule();
            Schedule();
        }
    }

    public static void StartupCheck()
    {
        using (TaskService taskService = new TaskService())
        {
            var task = taskService.RootFolder.AllTasks.FirstOrDefault(predicate: t => t.Name == taskName);
            if (task != null)
            {
                string strExeFilePath = Application.ExecutablePath.Trim();
                string action = task.Definition.Actions.FirstOrDefault()!.ToString().Trim();
                if (!strExeFilePath.Equals(value: action, comparisonType: StringComparison.OrdinalIgnoreCase) && !File.Exists(path: action))
                {
                    Logger.WriteLine(logMessage: "File doesn't exist: " + action);
                    Logger.WriteLine(logMessage: "Rescheduling to: " + strExeFilePath);
                    UnSchedule();
                    Schedule();
                }
            }
        }
    }

    public static void Schedule()
    {

        string strExeFilePath = Application.ExecutablePath;

        if (strExeFilePath is null) return;

        var userId = WindowsIdentity.GetCurrent().Name;

        using (TaskDefinition td = TaskService.Instance.NewTask())
        {

            td.RegistrationInfo.Description = "G-Helper Auto Start";
            td.Triggers.Add(unboundTrigger: new LogonTrigger { UserId = userId, Delay = TimeSpan.FromSeconds(value: 1) });
            td.Actions.Add(path: strExeFilePath);

            if (ProcessHelper.IsUserAdministrator())
            {
	            td.Principal.RunLevel = TaskRunLevel.Highest;
            }

            td.Settings.StopIfGoingOnBatteries = false;
            td.Settings.DisallowStartIfOnBatteries = false;
            td.Settings.ExecutionTimeLimit = TimeSpan.Zero;

            Debug.WriteLine(message: strExeFilePath);
            Debug.WriteLine(message: userId);

            try
            {
                TaskService.Instance.RootFolder.RegisterTaskDefinition(path: taskName, definition: td);
            }
            catch (Exception e)
            {
                if (ProcessHelper.IsUserAdministrator())
                    MessageBox.Show(text: "Can't create a start up task. Try running Task Scheduler by hand and manually deleting GHelper task if it exists there.", caption: "Scheduler Error", buttons: MessageBoxButtons.OK);
                else
                    ProcessHelper.RunAsAdmin();
            }
        }

    }

    public static void UnSchedule()
    {
        using (TaskService taskService = new TaskService())
        {
            try
            {
                taskService.RootFolder.DeleteTask(name: taskName);
            }
            catch (Exception e)
            {
                if (ProcessHelper.IsUserAdministrator())
                    MessageBox.Show(text: "Can't remove task. Try running Task Scheduler by hand and manually deleting GHelper task if it exists there.", caption: "Scheduler Error", buttons: MessageBoxButtons.OK);
                else
                    ProcessHelper.RunAsAdmin();
            }
        }
    }
}
