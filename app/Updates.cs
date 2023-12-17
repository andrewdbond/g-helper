using GHelper.UI;
using System.Diagnostics;
using System.Management;
using System.Net;
using System.Text.Json;

namespace GHelper
{

    public partial class Updates : RForm
    {
        const int DRIVER_NOT_FOUND = 2;
        const int DRIVER_NEWER = 1;

        //static int rowCount = 0;
        static string bios;
        static string model;

        static int updatesCount = 0;
        private static long lastUpdate;
        public struct DriverDownload
        {
            public string categoryName;
            public string title;
            public string version;
            public string downloadUrl;
            public string date;
            public JsonElement hardwares;
        }
        private void LoadUpdates(bool force = false)
        {

            if (!force && (Math.Abs(value: DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastUpdate) < 5000)) return;
            lastUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            (bios, model) = AppConfig.GetBiosAndModel();

            updatesCount = 0;
            labelUpdates.ForeColor = colorEco;
            labelUpdates.Text = Properties.Strings.NoNewUpdates;


            Text = Properties.Strings.BiosAndDriverUpdates + ": " + model + " " + bios;
            labelBIOS.Text = "BIOS";
            labelDrivers.Text = Properties.Strings.DriverAndSoftware;

            SuspendLayout();

            tableBios.Visible = false;
            tableDrivers.Visible = false;

            ClearTable(tableLayoutPanel: tableBios);
            ClearTable(tableLayoutPanel: tableDrivers);

            Task.Run(function: async () =>
            {
                DriversAsync(url: $"https://rog.asus.com/support/webapi/product/GetPDBIOS?website=global&model={model}&cpu=", type: 1, table: tableBios);
            });

            Task.Run(function: async () =>
            {
                DriversAsync(url: $"https://rog.asus.com/support/webapi/product/GetPDDrivers?website=global&model={model}&cpu={model}&osid=52", type: 0, table: tableDrivers);
            });
        }

        private void ClearTable(TableLayoutPanel tableLayoutPanel)
        {
            while (tableLayoutPanel.Controls.Count > 0)
            {
                tableLayoutPanel.Controls[index: 0].Dispose();
            }

            tableLayoutPanel.RowCount = 0;
        }

        public Updates()
        {
            InitializeComponent();
            InitTheme(setDPI: true);


            LoadUpdates(force: true);

            //buttonRefresh.Visible = false;
            buttonRefresh.Click += ButtonRefresh_Click;
            Shown += Updates_Shown;
        }

        private void ButtonRefresh_Click(object? sender, EventArgs e)
        {
            LoadUpdates();
        }

        private void Updates_Shown(object? sender, EventArgs e)
        {
            Height = Program.settingsForm.Height;
            Top = Program.settingsForm.Top;
            Left = Program.settingsForm.Left - Width - 5;
        }
        private Dictionary<string, string> GetDeviceVersions()
        {
            using (ManagementObjectSearcher objSearcher = new ManagementObjectSearcher(queryString: "Select * from Win32_PnPSignedDriver"))
            {
                using (ManagementObjectCollection objCollection = objSearcher.Get())
                {
                    Dictionary<string, string> list = new();

                    foreach (ManagementObject obj in objCollection)
                    {
                        if (obj[propertyName: "DeviceID"] is not null && obj[propertyName: "DriverVersion"] is not null)
                        {
	                        list[key: obj[propertyName: "DeviceID"].ToString()] = obj[propertyName: "DriverVersion"].ToString();
                        }
                    }

                    return list;
                }
            }
        }


        public void VisualiseDriver(DriverDownload driver, TableLayoutPanel table)
        {
            Invoke(method: delegate
            {
                string versionText = driver.version.Replace(oldValue: "latest version at the ", newValue: "");
                Label versionLabel = new Label { Text = versionText, Anchor = AnchorStyles.Left, AutoSize = true };
                versionLabel.Cursor = Cursors.Hand;
                versionLabel.Font = new Font(prototype: versionLabel.Font, newStyle: FontStyle.Underline);
                versionLabel.ForeColor = colorEco;
                versionLabel.Padding = new Padding(left: 5, top: 5, right: 5, bottom: 5);
                versionLabel.Click += delegate
                {
                    Process.Start(startInfo: new ProcessStartInfo(fileName: driver.downloadUrl) { UseShellExecute = true });
                };

                table.RowStyles.Add(rowStyle: new RowStyle(sizeType: SizeType.AutoSize));
                table.Controls.Add(control: new Label { Text = driver.categoryName, Anchor = AnchorStyles.Left, Dock = DockStyle.Fill, Padding = new Padding(left: 5, top: 5, right: 5, bottom: 5) }, column: 0, row: table.RowCount);
                table.Controls.Add(control: new Label { Text = driver.title, Anchor = AnchorStyles.Left, Dock = DockStyle.Fill, Padding = new Padding(left: 5, top: 5, right: 5, bottom: 5) }, column: 1, row: table.RowCount);
                table.Controls.Add(control: new Label { Text = driver.date, Anchor = AnchorStyles.Left, Dock = DockStyle.Fill, Padding = new Padding(left: 5, top: 5, right: 5, bottom: 5) }, column: 2, row: table.RowCount);
                table.Controls.Add(control: versionLabel, column: 3, row: table.RowCount);
                table.RowCount++;
            });
        }

        public void ShowTable(TableLayoutPanel table)
        {
            Invoke(method: delegate
            {
                table.Visible = true;
                ResumeLayout(performLayout: false);
                PerformLayout();
            });
        }

        public void VisualiseNewDriver(int position, int newer, TableLayoutPanel table)
        {
            var label = table.GetControlFromPosition(column: 3, row: position) as Label;
            if (label != null)
            {
                Invoke(method: delegate
                {
                    if (newer == DRIVER_NEWER)
                    {
                        label.Font = new Font(prototype: label.Font, newStyle: FontStyle.Underline | FontStyle.Bold);
                        label.ForeColor = colorTurbo;
                    }

                    if (newer == DRIVER_NOT_FOUND)
                    {
	                    label.ForeColor = Color.Gray;
                    }
                });
            }
        }

        public void VisualiseNewCount(int updatesCount, TableLayoutPanel table)
        {
            Invoke(method: delegate
            {
                labelUpdates.Text = $"{Properties.Strings.NewUpdates}: {updatesCount}";
                labelUpdates.ForeColor = colorTurbo;
                labelUpdates.Font = new Font(prototype: labelUpdates.Font, newStyle: FontStyle.Bold);
            });
        }

    public async void DriversAsync(string url, int type, TableLayoutPanel table)
        {

            try
            {
                using (var httpClient = new HttpClient(handler: new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.All
                }))
                {
                    httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd(input: "gzip, deflate, br");
                    httpClient.DefaultRequestHeaders.Add(name: "User-Agent", value: "C# App");
                    var json = await httpClient.GetStringAsync(requestUri: url);

                    var data = JsonSerializer.Deserialize<JsonElement>(json: json);
                    var groups = data.GetProperty(propertyName: "Result").GetProperty(propertyName: "Obj");


                    List<string> skipList = new() { "Armoury Crate & Aura Creator Installer", "MyASUS", "ASUS Smart Display Control", "Aura Wallpaper", "Virtual Pet", "ROG Font V1.5" };
                    List<DriverDownload> drivers = new();

                    for (int i = 0; i < groups.GetArrayLength(); i++)
                    {
                        var categoryName = groups[index: i].GetProperty(propertyName: "Name").ToString();
                        var files = groups[index: i].GetProperty(propertyName: "Files");

                        var oldTitle = "";

                        for (int j = 0; j < files.GetArrayLength(); j++)
                        {

                            var file = files[index: j];
                            var title = file.GetProperty(propertyName: "Title").ToString();

                            if (oldTitle != title && !skipList.Contains(item: title))
                            {

                                var driver = new DriverDownload();
                                driver.categoryName = categoryName;
                                driver.title = title;
                                driver.version = file.GetProperty(propertyName: "Version").ToString().Replace(oldValue: "V", newValue: "");
                                driver.downloadUrl = file.GetProperty(propertyName: "DownloadUrl").GetProperty(propertyName: "Global").ToString();
                                driver.hardwares = file.GetProperty(propertyName: "HardwareInfoList");
                                driver.date = file.GetProperty(propertyName: "ReleaseDate").ToString();
                                drivers.Add(item: driver);

                                VisualiseDriver(driver: driver, table: table);
                            }

                            oldTitle = title;
                        }
                    }

                    ShowTable(table: table);


                    Dictionary<string, string> devices = new();
                    if (type == 0)
                    {
	                    devices = this.GetDeviceVersions();
                    }

                    //Debug.WriteLine(biosVersion);

                    int count = 0;
                    foreach (var driver in drivers)
                    {
                        int newer = DRIVER_NOT_FOUND;
                        if (type == 0 && driver.hardwares.ToString().Length > 0)
                            for (int k = 0; k < driver.hardwares.GetArrayLength(); k++)
                            {
                                var deviceID = driver.hardwares[index: k].GetProperty(propertyName: "hardwareid").ToString();
                                var localVersions = devices.Where(predicate: p => p.Key.Contains(value: deviceID)).Select(selector: p => p.Value);
                                foreach (var localVersion in localVersions)
                                {
                                    newer = Math.Min(val1: newer, val2: new Version(version: driver.version).CompareTo(value: new Version(version: localVersion)));
                                    Logger.WriteLine(logMessage: driver.title + " " + deviceID  + " "+ driver.version + " vs " + localVersion + " = " + newer);
                                }

                            }

                        if (type == 1)
                        {
	                        newer = Int32.Parse(s: driver.version) > Int32.Parse(s: bios) ? 1 : -1;
                        }

                        VisualiseNewDriver(position: count, newer: newer, table: table);

                        if (newer == DRIVER_NEWER)
                        {
                            updatesCount++;
                            VisualiseNewCount(updatesCount: updatesCount, table: table);
                        }

                        count++;
                    }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine(logMessage: ex.ToString());

            }

        }
    }
}
