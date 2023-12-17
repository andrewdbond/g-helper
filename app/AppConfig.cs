using GHelper.Mode;
using System.Diagnostics;
using System.Management;
using System.Text.Json;

public static class AppConfig
{

    private static string configFile;

    private static string? _model;
    private static string? _modelShort;
    private static string? _bios;

    private static Dictionary<string, object> config = new Dictionary<string, object>();

    static AppConfig()
    {

        string startupPath = Application.StartupPath.Trim(trimChar: '\\');
        string appPath = Environment.GetFolderPath(folder: Environment.SpecialFolder.ApplicationData) + "\\GHelper";
        string configName = "\\config.json";

        if (File.Exists(path: startupPath + configName))
        {
            configFile = startupPath + configName;
        } else
        {
            configFile = appPath + configName;
        }


        if (!System.IO.Directory.Exists(path: appPath))
            System.IO.Directory.CreateDirectory(path: appPath);

        if (File.Exists(path: configFile))
        {
            string text = File.ReadAllText(path: configFile);
            try
            {
                config = JsonSerializer.Deserialize<Dictionary<string, object>>(json: text);
            }
            catch
            {
                Logger.WriteLine(logMessage: "Broken config: " + text);
                Init();
            }
        }
        else
        {
            Init();
        }

    }


    public static string GetModel()
    {
        if (_model is null)
        {
            _model = "";
            using (var searcher = new ManagementObjectSearcher(queryString: @"Select * from Win32_ComputerSystem"))
            {
                foreach (var process in searcher.Get())
                {
                    _model = process[propertyName: "Model"].ToString();
                    break;
                }
            }
        }

        return _model;
    }

    public static (string, string) GetBiosAndModel()
    {
        if (_bios is not null && _modelShort is not null) return (_bios, _modelShort);

        using (ManagementObjectSearcher objSearcher = new ManagementObjectSearcher(queryString: @"SELECT * FROM Win32_BIOS"))
        {
            using (ManagementObjectCollection objCollection = objSearcher.Get())
            {
                foreach (ManagementObject obj in objCollection)
                    if (obj[propertyName: "SMBIOSBIOSVersion"] is not null)
                    {
                        string[] results = obj[propertyName: "SMBIOSBIOSVersion"].ToString().Split(separator: ".");
                        if (results.Length > 1)
                        {
                            _modelShort = results[0];
                            _bios = results[1];
                        }
                        else
                        {
                            _modelShort = obj[propertyName: "SMBIOSBIOSVersion"].ToString();
                        }
                    }

                return (_bios, _modelShort);
            }
        }
    }

    public static string GetModelShort()
    {
        string model = GetModel();
        int trim = model.LastIndexOf(value: "_");
        if (trim > 0)
        {
	        model = model.Substring(startIndex: 0, length: trim);
        }

        return model;
    }

    public static bool ContainsModel(string contains)
    {
        GetModel();
        return (_model is not null && _model.ToLower().Contains(value: contains.ToLower()));
    }


    private static void Init()
    {
        config = new Dictionary<string, object>();
        config[key: "performance_mode"] = 0;
        string jsonString = JsonSerializer.Serialize(value: config);
        File.WriteAllText(path: configFile, contents: jsonString);
    }

    public static int Get(string name, int empty = -1)
    {
        if (config.ContainsKey(key: name))
        {
            //Debug.WriteLine(name);
            return int.Parse(s: config[key: name].ToString());
        }
        else
        {
            //Debug.WriteLine(name + "E");
            return empty;
        }
    }

    public static bool Is(string name)
    {
        return Get(name: name) == 1;
    }

    public static bool IsNotFalse(string name)
    {
        return Get(name: name) != 0;
    }

    public static string GetString(string name, string empty = null)
    {
        if (config.ContainsKey(key: name))
            return config[key: name].ToString();
        else return empty;
    }

    private static void Write()
    {
        string jsonString = JsonSerializer.Serialize(value: config, options: new JsonSerializerOptions { WriteIndented = true });
        try
        {
            File.WriteAllText(path: configFile, contents: jsonString);
        }
        catch (Exception e)
        {
            Debug.Write(message: e.ToString());
        }
    }

    public static void Set(string name, int value)
    {
        config[key: name] = value;
        Write();
    }

    public static void Set(string name, string value)
    {
        config[key: name] = value;
        Write();
    }
    public static void Remove(string name)
    {
        config.Remove(key: name);
        Write();
    }

    public static void RemoveMode(string name)
    {
        Remove(name: name + "_" + Modes.GetCurrent());
    }

    public static string GgetParamName(AsusFan device, string paramName = "fan_profile")
    {
        int mode = Modes.GetCurrent();
        string name;

        switch (device)
        {
            case AsusFan.GPU:
                name = "gpu";
                break;
            case AsusFan.Mid:
                name = "mid";
                break;
            case AsusFan.XGM:
                name = "xgm";
                break;
            default:
                name = "cpu";
                break;

        }

        return paramName + "_" + name + "_" + mode;
    }

    public static byte[] GetFanConfig(AsusFan device)
    {
        string curveString = GetString(name: GgetParamName(device: device));
        byte[] curve = { };

        if (curveString is not null)
        {
	        curve = StringToBytes(str: curveString);
        }

        return curve;
    }

    public static void SetFanConfig(AsusFan device, byte[] curve)
    {
        string bitCurve = BitConverter.ToString(value: curve);
        Set(name: GgetParamName(device: device), value: bitCurve);
    }


    public static byte[] StringToBytes(string str)
    {
        String[] arr = str.Split(separator: '-');
        byte[] array = new byte[arr.Length];
        for (int i = 0; i < arr.Length; i++)
        {
	        array[i] = Convert.ToByte(value: arr[i], fromBase: 16);
        }

        return array;
    }

    public static byte[] GetDefaultCurve(AsusFan device)
    {
        int mode = Modes.GetCurrentBase();
        byte[] curve;

        switch (mode)
        {
            case 1:
                if (device == AsusFan.GPU)
                {
	                curve = StringToBytes(str: "14-3F-44-48-4C-50-54-62-16-1F-26-2D-39-47-55-5F");
                }
                else
                {
	                curve = StringToBytes(str: "14-3F-44-48-4C-50-54-62-11-1A-22-29-34-43-51-5A");
                }

                break;
            case 2:
                if (device == AsusFan.GPU)
                {
	                curve = StringToBytes(str: "3C-41-42-46-47-4B-4C-62-08-11-11-1D-1D-26-26-2D");
                }
                else
                {
	                curve = StringToBytes(str: "3C-41-42-46-47-4B-4C-62-03-0C-0C-16-16-22-22-29");
                }

                break;
            default:
                if (device == AsusFan.GPU)
                {
	                curve = StringToBytes(str: "3A-3D-40-44-48-4D-51-62-0C-16-1D-1F-26-2D-34-4A");
                }
                else
                {
	                curve = StringToBytes(str: "3A-3D-40-44-48-4D-51-62-08-11-16-1A-22-29-30-45");
                }

                break;
        }

        return curve;
    }

    public static string GetModeString(string name)
    {
        return GetString(name: name + "_" + Modes.GetCurrent());
    }

    public static int GetMode(string name, int empty = -1)
    {
        return Get(name: name + "_" + Modes.GetCurrent(), empty: empty);
    }

    public static bool IsMode(string name)
    {
        return Get(name: name + "_" + Modes.GetCurrent()) == 1;
    }

    public static void SetMode(string name, int value)
    {
        Set(name: name + "_" + Modes.GetCurrent(), value: value);
    }

    public static void SetMode(string name, string value)
    {
        Set(name: name + "_" + Modes.GetCurrent(), value: value);
    }

    public static bool IsAlly()
    {
        return ContainsModel(contains: "RC71");
    }

    public static bool NoMKeys()
    {
        return (ContainsModel(contains: "Z13") && !IsARCNM()) ||
               ContainsModel(contains: "FX706") ||
               ContainsModel(contains: "FA506") ||
               ContainsModel(contains: "FX506") ||
               ContainsModel(contains: "Duo") ||
               ContainsModel(contains: "FX505");
    }

    public static bool IsARCNM()
    {
        return ContainsModel(contains: "GZ301VIC");
    }

    public static bool IsTUF()
    {
        return ContainsModel(contains: "TUF");
    }

    public static bool IsVivobook()
    {
        return ContainsModel(contains: "Vivobook");
    }

    // Devices with bugged bios command to change brightness
    public static bool SwappedBrightness()
    {
        return ContainsModel(contains: "FA506IH") || ContainsModel(contains: "FA506IC") || ContainsModel(contains: "FX506LU") || ContainsModel(contains: "FX506IC") || ContainsModel(contains: "FX506LH");
    }


    public static bool IsDUO()
    {
        return ContainsModel(contains: "Duo");
    }

    // G14 2020 has no aura, but media keys instead
    public static bool NoAura()
    {
        return ContainsModel(contains: "GA401I") && !ContainsModel(contains: "GA401IHR");
    }

    public static bool IsSingleColor()
    {
        return  ContainsModel(contains: "GA401") || ContainsModel(contains: "FX517Z") || ContainsModel(contains: "FX516P") || ContainsModel(contains: "X13") || IsARCNM() || ContainsModel(contains: "GA502IU");
    }

    public static bool IsStrix()
    {
        return ContainsModel(contains: "Strix") || ContainsModel(contains: "Scar");
    }

    public static bool IsStrixLimitedRGB()
    {
        return ContainsModel(contains: "G614JV") || ContainsModel(contains: "G614JZ") || ContainsModel(contains: "G512LI") || ContainsModel(contains: "G513R") || ContainsModel(contains: "G713PV") || ContainsModel(contains: "G513IE") || ContainsModel(contains: "G513QM") || ContainsModel(contains: "G713RC");
    }

    public static bool IsStrixNumpad()
    {
        return ContainsModel(contains: "G713R");
    }

    public static bool IsZ13()
    {
        return ContainsModel(contains: "Z13");
    }

    public static bool HasTabletMode()
    {
        return ContainsModel(contains: "X16") || ContainsModel(contains: "X13");
    }

    public static bool IsX13()
    {
        return ContainsModel(contains: "X13");
    }


    public static bool IsAdvantageEdition()
    {
        return ContainsModel(contains: "13QY");
    }

    public static bool NoAutoUltimate()
    {
        return ContainsModel(contains: "G614") || ContainsModel(contains: "GU604") || ContainsModel(contains: "FX507") || ContainsModel(contains: "G513") || ContainsModel(contains: "FA617");
    }


    public static bool IsManualModeRequired()
    {
        if (!IsMode(name: "auto_apply_power"))
            return false;

        return
            Is(name: "manual_mode") ||
            ContainsModel(contains: "GU604") ||
            ContainsModel(contains: "G733") ||
            ContainsModel(contains: "FX507Z");
    }

    public static bool IsFanScale()
    {
        if (!ContainsModel(contains: "GU604")) return false; 

        try
        {
            var (bios, model) = GetBiosAndModel();
            return (Int32.Parse(s: bios) < 312);
        } catch
        {
            return false;
        }
    }

    public static bool IsFanRequired()
    {
        return ContainsModel(contains: "GA402X") || ContainsModel(contains: "G513") || ContainsModel(contains: "G713R") || ContainsModel(contains: "G713P");
    }

    public static bool IsPowerRequired()
    {
        return ContainsModel(contains: "FX507") || ContainsModel(contains: "FX517") || ContainsModel(contains: "FX707");
    }

    public static bool IsGPUFixNeeded()
    {
        return ContainsModel(contains: "GA402X") || ContainsModel(contains: "GV302") || ContainsModel(contains: "GV301") || ContainsModel(contains: "GZ301") || ContainsModel(contains: "FX506") || ContainsModel(contains: "GU603") || ContainsModel(contains: "GU604") || ContainsModel(contains: "G614J") || ContainsModel(contains: "GA503");
    }

    public static bool IsGPUFix()
    {
        return Is(name: "gpu_fix") || (ContainsModel(contains: "GA402X") && IsNotFalse(name: "gpu_fix"));
    }

    public static bool IsForceSetGPUMode()
    {
        return Is(name: "gpu_mode_force_set") || ContainsModel(contains: "503");
    }

    public static bool IsNoGPUModes()
    {
        return ContainsModel(contains: "GV301RA") || ContainsModel(contains: "GV302XA") || IsAlly();
    }

    public static bool IsHardwareTouchpadToggle()
    {
        return ContainsModel(contains: "FA507");
    }

    public static bool IsASUS()
    {
        return ContainsModel(contains: "ROG") || ContainsModel(contains: "TUF") || ContainsModel(contains: "Vivobook") || ContainsModel(contains: "Zenbook");
    }
}
