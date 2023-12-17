using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace GHelper.AutoUpdate
{
    public class AutoUpdateControl
    {

        SettingsForm settings;

        public string versionUrl = "http://github.com/seerge/g-helper/releases";
        static long lastUpdate;

        public AutoUpdateControl(SettingsForm settingsForm)
        {
            settings = settingsForm;
            var appVersion = new Version(version: Assembly.GetExecutingAssembly().GetName().Version.ToString());
            settings.SetVersionLabel(label: Properties.Strings.VersionLabel + $": {appVersion.Major}.{appVersion.Minor}.{appVersion.Build}");
        }

        public void CheckForUpdates()
        {
            // Run update once per 12 hours
            if (Math.Abs(value: DateTimeOffset.Now.ToUnixTimeSeconds() - lastUpdate) < 43200) return;
            lastUpdate = DateTimeOffset.Now.ToUnixTimeSeconds();

            Task.Run(function: async () =>
            {
                await Task.Delay(delay: TimeSpan.FromSeconds(value: 1));
                CheckForUpdatesAsync();
            });
        }

        public void LoadReleases()
        {
            Process.Start(startInfo: new ProcessStartInfo(fileName: versionUrl) { UseShellExecute = true });
        }

        async void CheckForUpdatesAsync()
        {

            if (AppConfig.Is(name: "skip_updates")) return;

            try
            {

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add(name: "User-Agent", value: "C# App");
                    var json = await httpClient.GetStringAsync(requestUri: "https://api.github.com/repos/seerge/g-helper/releases/latest");
                    var config = JsonSerializer.Deserialize<JsonElement>(json: json);
                    var tag = config.GetProperty(propertyName: "tag_name").ToString().Replace(oldValue: "v", newValue: "");
                    var assets = config.GetProperty(propertyName: "assets");

                    string url = null;

                    for (int i = 0; i < assets.GetArrayLength(); i++)
                    {
                        if (assets[index: i].GetProperty(propertyName: "browser_download_url").ToString().Contains(value: ".zip"))
                            url = assets[index: i].GetProperty(propertyName: "browser_download_url").ToString();
                    }

                    if (url is null)
                        url = assets[index: 0].GetProperty(propertyName: "browser_download_url").ToString();

                    var gitVersion = new Version(version: tag);
                    var appVersion = new Version(version: Assembly.GetExecutingAssembly().GetName().Version.ToString());
                    //appVersion = new Version("0.50.0.0"); 

                    if (gitVersion.CompareTo(value: appVersion) > 0)
                    {
                        versionUrl = url;
                        settings.SetVersionLabel(label: Properties.Strings.DownloadUpdate + ": " + tag, update: true);

                        if (AppConfig.GetString(name: "skip_version") != tag)
                        {
                            DialogResult dialogResult = MessageBox.Show(text: Properties.Strings.DownloadUpdate + ": G-Helper " + tag + "?", caption: "Update", buttons: MessageBoxButtons.YesNo);
                            if (dialogResult == DialogResult.Yes)
                                AutoUpdate(requestUri: url);
                            else
                                AppConfig.Set(name: "skip_version", value: tag);
                        }

                    }
                    else
                    {
                        Logger.WriteLine(logMessage: $"Latest version {appVersion}");
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine(logMessage: "Failed to check for updates:" + ex.Message);
            }

        }


        async void AutoUpdate(string requestUri)
        {

            Uri uri = new Uri(uriString: requestUri);
            string zipName = Path.GetFileName(path: uri.LocalPath);

            string exeLocation = Application.ExecutablePath;
            string exeDir = Path.GetDirectoryName(path: exeLocation);
            string zipLocation = exeDir + "\\" + zipName;

            using (WebClient client = new WebClient())
            {
                client.DownloadFile(address: uri, fileName: zipLocation);

                Logger.WriteLine(logMessage: requestUri);
                Logger.WriteLine(logMessage: zipLocation);
                Logger.WriteLine(logMessage: exeLocation);

                var cmd = new Process();
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.FileName = "powershell";
                cmd.StartInfo.Arguments = $"Start-Sleep -Seconds 1; Expand-Archive {zipLocation} -DestinationPath {exeDir} -Force; Remove-Item {zipLocation} -Force; {exeLocation}";
                cmd.Start();

                Application.Exit();
            }

        }

    }
}
