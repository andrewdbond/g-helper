using GHelper.Gpu;
using GHelper.Helpers;
using GHelper.Input;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace GHelper.USB
{
    public class AuraPower
    {
        public bool BootLogo;
        public bool BootKeyb;
        public bool AwakeLogo;
        public bool AwakeKeyb;
        public bool SleepLogo;
        public bool SleepKeyb;
        public bool ShutdownLogo;
        public bool ShutdownKeyb;

        public bool BootBar;
        public bool AwakeBar;
        public bool SleepBar;
        public bool ShutdownBar;

        public bool BootLid;
        public bool AwakeLid;
        public bool SleepLid;
        public bool ShutdownLid;

        public bool BootRear;
        public bool AwakeRear;
        public bool SleepRear;
        public bool ShutdownRear;
    }

    public enum AuraMode : int
    {
        AuraStatic = 0,
        AuraBreathe = 1,
        AuraColorCycle = 2,
        AuraRainbow = 3,
        Star = 4,
        Rain = 5,
        Highlight = 6,
        Laser = 7,
        Ripple = 8,
        AuraStrobe = 10,
        Comet = 11,
        Flash = 12,
        HEATMAP = 20,
        GPUMODE = 21,
        AMBIENT = 22,
    }

    public enum AuraSpeed : int
    {
        Slow = 0,
        Normal = 1,
        Fast = 2,
    }


    public static class Aura
    {

        static byte[] MESSAGE_APPLY = { AsusHid.AURA_ID, 0xb4 };
        static byte[] MESSAGE_SET = { AsusHid.AURA_ID, 0xb5, 0, 0, 0 };

        static readonly int AURA_ZONES = 8;

        private static AuraMode mode = AuraMode.AuraStatic;
        private static AuraSpeed speed = AuraSpeed.Normal;

        public static Color Color1 = Color.White;
        public static Color Color2 = Color.Black;

        static bool isACPI = AppConfig.IsTUF() || AppConfig.IsVivobook();
        static bool isStrix = AppConfig.IsStrix();

        static bool isStrix4Zone = AppConfig.IsStrixLimitedRGB();
        static bool isStrixNumpad = AppConfig.IsStrixNumpad();

        static public bool isSingleColor = false;

        static bool isOldHeatmap = AppConfig.Is(name: "old_heatmap");

        static System.Timers.Timer timer = new System.Timers.Timer(interval: 1000);

        private static Dictionary<AuraMode, string> _modesSingleColor = new Dictionary<AuraMode, string>
        {
            { AuraMode.AuraStatic, Properties.Strings.AuraStatic },
            { AuraMode.AuraBreathe, Properties.Strings.AuraBreathe },
            { AuraMode.AuraStrobe, Properties.Strings.AuraStrobe },
        };

        private static Dictionary<AuraMode, string> _modes = new Dictionary<AuraMode, string>
        {
            { AuraMode.AuraStatic, Properties.Strings.AuraStatic },
            { AuraMode.AuraBreathe, Properties.Strings.AuraBreathe },
            { AuraMode.AuraColorCycle, Properties.Strings.AuraColorCycle },
            { AuraMode.AuraRainbow, Properties.Strings.AuraRainbow },
            { AuraMode.AuraStrobe, Properties.Strings.AuraStrobe },
            { AuraMode.HEATMAP, "Heatmap"},
            { AuraMode.GPUMODE, "GPU Mode" },
            { AuraMode.AMBIENT, "Ambient"},
        };

        private static Dictionary<AuraMode, string> _modesStrix = new Dictionary<AuraMode, string>
        {
            { AuraMode.AuraStatic, Properties.Strings.AuraStatic },
            { AuraMode.AuraBreathe, Properties.Strings.AuraBreathe },
            { AuraMode.AuraColorCycle, Properties.Strings.AuraColorCycle },
            { AuraMode.AuraRainbow, Properties.Strings.AuraRainbow },
            { AuraMode.Star, "Star" },
            { AuraMode.Rain, "Rain" },
            { AuraMode.Highlight, "Highlight" },
            { AuraMode.Laser, "Laser" },
            { AuraMode.Ripple, "Ripple" },
            { AuraMode.AuraStrobe, Properties.Strings.AuraStrobe},
            { AuraMode.Comet, "Comet" },
            { AuraMode.Flash, "Flash" },
            { AuraMode.HEATMAP, "Heatmap"},
            { AuraMode.AMBIENT, "Ambient"},
        };

        static Aura()
        {
            timer.Elapsed += Timer_Elapsed;
            isSingleColor = AppConfig.IsSingleColor(); // Mono Color

            if (AppConfig.ContainsModel(contains: "GA402X") || AppConfig.ContainsModel(contains: "GA402N"))
            {
                var device = AsusHid.FindDevices(reportId: AsusHid.AURA_ID).FirstOrDefault();
                if (device is null) return;
                Logger.WriteLine(logMessage: $"USB Version: {device.ReleaseNumberBcd} {device.ReleaseNumber}");

                if (device.ReleaseNumberBcd >= 22 && device.ReleaseNumberBcd <= 25)
                {
	                isSingleColor = true;
                }
            }
        }

        public static Dictionary<AuraSpeed, string> GetSpeeds()
        {
            return new Dictionary<AuraSpeed, string>
            {
                { AuraSpeed.Slow, Properties.Strings.AuraSlow },
                { AuraSpeed.Normal, Properties.Strings.AuraNormal },
                { AuraSpeed.Fast, Properties.Strings.AuraFast }
            };
        }


        public static Dictionary<AuraMode, string> GetModes()
        {
            if (isACPI)
            {
                _modes.Remove(key: AuraMode.AuraRainbow);
            }

            if (isSingleColor)
            {
                return _modesSingleColor;
            }

            if (AppConfig.IsAdvantageEdition())
            {
                return _modes;
            }

            if (AppConfig.IsStrix() && !AppConfig.IsStrixLimitedRGB())
            {
                return _modesStrix;
            }

            return _modes;
        }

        public static AuraMode Mode
        {
            get { return mode; }
            set
            {
                mode = GetModes().ContainsKey(key: value) ? value : AuraMode.AuraStatic;
            }
        }

        public static AuraSpeed Speed
        {
            get { return speed; }
            set
            {
                speed = GetSpeeds().ContainsKey(key: value) ? value : AuraSpeed.Normal;
            }

        }

        public static void SetColor(int colorCode)
        {
            Color1 = Color.FromArgb(argb: colorCode);
        }

        public static void SetColor2(int colorCode)
        {
            Color2 = Color.FromArgb(argb: colorCode);
        }

        public static bool HasSecondColor()
        {
            return mode == AuraMode.AuraBreathe && !isACPI;
        }

        private static void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (!InputDispatcher.backlightActivity)
                return;

            if (Mode == AuraMode.HEATMAP)
            {
                CustomRGB.ApplyHeatmap();
            }
            else if (Mode == AuraMode.AMBIENT)
            {
                CustomRGB.ApplyAmbient();
            }
        }


        public static byte[] AuraMessage(AuraMode mode, Color color, Color color2, int speed, bool mono = false)
        {

            byte[] msg = new byte[17];
            msg[0] = AsusHid.AURA_ID;
            msg[1] = 0xb3;
            msg[2] = 0x00; // Zone 
            msg[3] = (byte)mode; // Aura Mode
            msg[4] = color.R; // R
            msg[5] = mono ? (byte)0 : color.G; // G
            msg[6] = mono ? (byte)0 : color.B; // B
            msg[7] = (byte)speed; // aura.speed as u8;
            msg[8] = 0; // aura.direction as u8;
            msg[9] = mode == AuraMode.AuraBreathe ? (byte)1 : (byte)0;
            msg[10] = color2.R; // R
            msg[11] = mono ? (byte)0 : color2.G; // G
            msg[12] = mono ? (byte)0 : color2.B; // B
            return msg;
        }

        public static void Init()
        {
            Task.Run(function: async () =>
            {
                AsusHid.Write(dataList: new List<byte[]> {
                    new byte[] { AsusHid.AURA_ID, 0xb9 },
                    Encoding.ASCII.GetBytes(s: "]ASUS Tech.Inc."),
                    new byte[] { AsusHid.AURA_ID, 0x05, 0x20, 0x31, 0, 0x1a },
                    //Encoding.ASCII.GetBytes("^ASUS Tech.Inc."),
                    //new byte[] { 0x5e, 0x05, 0x20, 0x31, 0, 0x1a }
                });
            });
        }


        public static void ApplyBrightness(int brightness, string log = "Backlight", bool delay = false)
        {
            Task.Run(function: async () =>
            {
                if (delay) await Task.Delay(delay: TimeSpan.FromSeconds(value: 1));
                if (isACPI) Program.acpi.TUFKeyboardBrightness(brightness: brightness);

                AsusHid.Write(data: new byte[] { AsusHid.AURA_ID, 0xba, 0xc5, 0xc4, (byte)brightness }, log: log);
                if (AppConfig.ContainsModel(contains: "GA503"))
                    AsusHid.WriteInput(data: new byte[] { AsusHid.INPUT_ID, 0xba, 0xc5, 0xc4, (byte)brightness }, log: log);
            });


        }

        static byte[] AuraPowerMessage(AuraPower flags)
        {
            byte keyb = 0, bar = 0, lid = 0, rear = 0;

            if (flags.BootLogo)
            {
	            keyb |= 1 << 0;
            }

            if (flags.BootKeyb)
            {
	            keyb |= 1 << 1;
            }

            if (flags.AwakeLogo)
            {
	            keyb |= 1 << 2;
            }

            if (flags.AwakeKeyb)
            {
	            keyb |= 1 << 3;
            }

            if (flags.SleepLogo)
            {
	            keyb |= 1 << 4;
            }

            if (flags.SleepKeyb)
            {
	            keyb |= 1 << 5;
            }

            if (flags.ShutdownLogo)
            {
	            keyb |= 1 << 6;
            }

            if (flags.ShutdownKeyb)
            {
	            keyb |= 1 << 7;
            }

            if (flags.BootBar)
            {
	            bar |= 1 << 1;
            }

            if (flags.AwakeBar)
            {
	            bar |= 1 << 2;
            }

            if (flags.SleepBar)
            {
	            bar |= 1 << 3;
            }

            if (flags.ShutdownBar)
            {
	            bar |= 1 << 4;
            }

            if (flags.BootLid)
            {
	            lid |= 1 << 0;
            }

            if (flags.AwakeLid)
            {
	            lid |= 1 << 1;
            }

            if (flags.SleepLid)
            {
	            lid |= 1 << 2;
            }

            if (flags.ShutdownLid)
            {
	            lid |= 1 << 3;
            }

            if (flags.BootLid)
            {
	            lid |= 1 << 4;
            }

            if (flags.AwakeLid)
            {
	            lid |= 1 << 5;
            }

            if (flags.SleepLid)
            {
	            lid |= 1 << 6;
            }

            if (flags.ShutdownLid)
            {
	            lid |= 1 << 7;
            }

            if (flags.BootRear)
            {
	            rear |= 1 << 0;
            }

            if (flags.AwakeRear)
            {
	            rear |= 1 << 1;
            }

            if (flags.SleepRear)
            {
	            rear |= 1 << 2;
            }

            if (flags.ShutdownRear)
            {
	            rear |= 1 << 3;
            }

            if (flags.BootRear)
            {
	            rear |= 1 << 4;
            }

            if (flags.AwakeRear)
            {
	            rear |= 1 << 5;
            }

            if (flags.SleepRear)
            {
	            rear |= 1 << 6;
            }

            if (flags.ShutdownRear)
            {
	            rear |= 1 << 7;
            }

            return new byte[] { 0x5d, 0xbd, 0x01, keyb, bar, lid, rear, 0xFF };
        }

        public static void ApplyPower()
        {

            AuraPower flags = new();

            // Keyboard
            flags.AwakeKeyb = AppConfig.IsNotFalse(name: "keyboard_awake");
            flags.BootKeyb = AppConfig.IsNotFalse(name: "keyboard_boot");
            flags.SleepKeyb = AppConfig.IsNotFalse(name: "keyboard_sleep");
            flags.ShutdownKeyb = AppConfig.IsNotFalse(name: "keyboard_shutdown");

            // Logo
            flags.AwakeLogo = AppConfig.IsNotFalse(name: "keyboard_awake_logo");
            flags.BootLogo = AppConfig.IsNotFalse(name: "keyboard_boot_logo");
            flags.SleepLogo = AppConfig.IsNotFalse(name: "keyboard_sleep_logo");
            flags.ShutdownLogo = AppConfig.IsNotFalse(name: "keyboard_shutdown_logo");

            // Lightbar
            flags.AwakeBar = AppConfig.IsNotFalse(name: "keyboard_awake_bar");
            flags.BootBar = AppConfig.IsNotFalse(name: "keyboard_boot_bar");
            flags.SleepBar = AppConfig.IsNotFalse(name: "keyboard_sleep_bar");
            flags.ShutdownBar = AppConfig.IsNotFalse(name: "keyboard_shutdown_bar");

            // Lid
            flags.AwakeLid = AppConfig.IsNotFalse(name: "keyboard_awake_lid");
            flags.BootLid = AppConfig.IsNotFalse(name: "keyboard_boot_lid");
            flags.SleepLid = AppConfig.IsNotFalse(name: "keyboard_sleep_lid");
            flags.ShutdownLid = AppConfig.IsNotFalse(name: "keyboard_shutdown_lid");

            // Rear Bar
            flags.AwakeRear = AppConfig.IsNotFalse(name: "keyboard_awake_lid");
            flags.BootRear = AppConfig.IsNotFalse(name: "keyboard_boot_lid");
            flags.SleepRear = AppConfig.IsNotFalse(name: "keyboard_sleep_lid");
            flags.ShutdownRear = AppConfig.IsNotFalse(name: "keyboard_shutdown_lid");

            AsusHid.Write(data: AuraPowerMessage(flags: flags));

            if (isACPI)
                Program.acpi.TUFKeyboardPower(
                    awake: flags.AwakeKeyb,
                    boot: flags.BootKeyb,
                    sleep: flags.SleepKeyb,
                    shutdown: flags.ShutdownKeyb);

        }

        static byte[] packetMap = new byte[]
        {
                    /* VDN   VUP   MICM  HPFN  ARMC  */
                         2,    3,    4,    5,    6,
        /* ESC          F1    F2    F3    F4    F5    F6    F7    F8    F9   F10   F11   F12              DEL15 DEL17  PAUS  PRT   HOME  */
            21,         23,   24,   25,   26,   28,   29,   30,   31,   33,   34,   35,   36,               37,   38,   39,   40,   41,
        /* BKTK    1     2     3     4     5     6     7     8     9     0     -     =   BSPC  BSPC  BSPC PLY15  NMLK  NMDV  NMTM  NMMI  */
            42,   43,   44,   45,   46,   47,   48,   49,   50,   51,   52,   53,   54,   55,   56,   57,   58,   59,   60,   61,   62,
        /* TAB     Q     W     E     R     T     Y     U     I     O     P     [     ]     \              STP15  NM7   NM8   NM9   NMPL  */
            63,   64,   65,   66,   67,   68,   69,   70,   71,   72,   73,   74,   75,   76,               79,   80,   81,   82,   83,
        /* CPLK    A     S     D     F     G     H     J     K     L     ;     "     #   ENTR  ENTR  ENTR PRV15  NM4   NM5   NM6   NMPL  */
            84,   85,   86,   87,   88,   89,   90,   91,   92,   93,   94,   95,   96,   97,   98,   99,  100,  101,  102,  103,  104,
        /* LSFT  ISO\    Z     X     C     V     B     N     M     ,     .     /   RSFT  RSFT  RSFT  ARWU NXT15  NM1   NM2   NM3   NMER  */
           105,  106,  107,  108,  109,  110,  111,  112,  113,  114,  115,  116,  117,  118,  119,  139,  121,  122,  123,  124,  125,
        /* LCTL  LFNC  LWIN  LALT              SPC               RALT  RFNC  RCTL        ARWL  ARWD  ARWR PRT15        NM0   NMPD  NMER  */
           126,  127,  128,  129,              131,              135,  136,  137,        159,  160,  161,  142,        144,  145,  146,
        /* LB1   LB2   LB3                                                                                             LB4   LB5   LB6   */
           174,  173,  172,                                                                                            171,  170,  169,
        /* KSTN  LOGO  LIDL  LIDR  */
             0,  167,  176,  177,

        };


        static byte[] packetZone = new byte[]
        {
                    /* VDN   VUP   MICM  HPFN  ARMC  */
                         0,    0,    1,    1,    1,
        /* ESC          F1    F2    F3    F4    F5    F6    F7    F8    F9   F10   F11   F12              DEL15 DEL17  PAUS  PRT   HOM   */
             0,          0,    0,    1,    1,    1,    1,    2,    2,    2,    2,    3,    3,                3,   3,    3,    3,    3,
        /* BKTK    1     2     3     4     5     6     7     8     9     0     -     =   BSPC  BSPC  BSPC PLY15  NMLK  NMDV  NMTM  NMMI  */
             0,    0,    0,    0,    1,    1,    1,    1,    2,    2,    2,    2,    3,    3,    3,    3,    3,    3,    3,    3,    3,
        /* TAB     Q     W     E     R     T     Y     U     I     O     P     [     ]     \              STP15  NM7   NM8   NM9   NMPL  */
             0,    0,    0,    0,    1,    1,    1,    1,    2,    2,    2,    2,    3,    3,                3,    3,    3,    3,    3,
        /* CPLK    A     S     D     F     G     H     J     K     L     ;     "     #   ENTR  ENTR  ENTR PRV15  NM4   NM5   NM6   NMPL  */
             0,    0,    0,    0,    1,    1,    1,    1,    2,    2,    2,    2,    3,    3,    3,    3,    3,    3,    3,    3,    3,
        /* LSFT  ISO\    Z     X     C     V     B     N     M     ,     .     /   RSFT  RSFT  RSFT  ARWU NXT15  NM1   NM2   NM3   NMER  */
             0,    0,    0,    0,    1,    1,    1,    1,    2,    2,    2,    2,    3,    3,    3,    3,    3,    3,    3,    3,    3,
        /* LCTL  LFNC  LWIN  LALT              SPC               RALT  RFNC  RCTL        ARWL  ARWD  ARWR PRT15        NM0   NMPD  NMER  */
             0,    0,    0,    0,              1,                  2,    2,    2,          3,    3,    3,    3,          3,    3,    3,
        /* LB1   LB1   LB3                                                                                             LB4   LB5   LB6   */
             5,    5,    4,                                                                                              6,    7,    7,
        /* KSTN  LOGO  LIDL  LIDR  */
             3,    0,    0,    3,

        };


        static byte[] packetZoneNumpad = new byte[]
        {
                    /* VDN   VUP   MICM  HPFN  ARMC  */
                         0,    0,    0,    1,    1,
        /* ESC          F1    F2    F3    F4    F5    F6    F7    F8    F9   F10   F11   F12              DEL15 DEL17  PAUS  PRT   HOM   */
             0,          0,    0,    0,    1,    1,    1,    1,    1,    2,    2,    2,    2,                3,   3,    3,    3,    3,
        /* BKTK    1     2     3     4     5     6     7     8     9     0     -     =   BSPC  BSPC  BSPC PLY15  NMLK  NMDV  NMTM  NMMI  */
             0,    0,    0,    0,    0,    1,    1,    1,    1,    1,    2,    2,    2,    2,    2,    2,    3,    3,    3,    3,    3,
        /* TAB     Q     W     E     R     T     Y     U     I     O     P     [     ]     \              STP15  NM7   NM8   NM9   NMPL  */
             0,    0,    0,    0,    0,    1,    1,    1,    1,    1,    2,    2,    2,    2,                3,    3,    3,    3,    3,
        /* CPLK    A     S     D     F     G     H     J     K     L     ;     "     #   ENTR  ENTR  ENTR PRV15  NM4   NM5   NM6   NMPL  */
             0,    0,    0,    0,    0,    1,    1,    1,    1,    1,    2,    2,    2,    2,    2,    2,    3,    3,    3,    3,    3,
        /* LSFT  ISO\    Z     X     C     V     B     N     M     ,     .     /   RSFT  RSFT  RSFT  ARWU NXT15  NM1   NM2   NM3   NMER  */
             0,    0,    0,    0,    0,    1,    1,    1,    1,    1,    2,    2,    2,    2,    2,     2,   3,    3,    3,    3,    3,
        /* LCTL  LFNC  LWIN  LALT              SPC               RALT  RFNC  RCTL        ARWL  ARWD  ARWR PRT15        NM0   NMPD  NMER  */
             0,    0,    0,    0,              1,                  1,    2,    2,          2,    2,    2,    3,          3,    3,    3,
        /* LB1   LB1   LB3                                                                                             LB4   LB5   LB6   */
             5,    5,    4,                                                                                              6,    7,    7,
        /* KSTN  LOGO  LIDL  LIDR  */
             3,    0,    0,    3,

        };

        static byte[] packet4Zone = new byte[]
        {
/*01        Z1  Z2  Z3  Z4  NA  NA  KeyZone */
            0,  1,  2,  3,  0,  0, 

/*02        RR  R   RM  LM  L   LL  LighBar */
            7,  7,  6,  5,  4,  4,

        };


        public static void ApplyDirect(Color[] color, bool init = false)
        {
            const byte keySet = 167;
            const byte ledCount = 178;
            const ushort mapSize = 3 * ledCount;
            const byte ledsPerPacket = 16;

            byte[] buffer = new byte[64];
            byte[] keyBuf = new byte[mapSize];

            buffer[0] = AsusHid.AURA_ID;
            buffer[1] = 0xbc;
            buffer[2] = 0;
            buffer[3] = 1;
            buffer[4] = 1;
            buffer[5] = 1;
            buffer[6] = 0;
            buffer[7] = 0x10;

            if (init)
            {
                Init();
                AsusHid.WriteAura(data: new byte[] { AsusHid.AURA_ID, 0xbc });
            }

            Array.Clear(array: keyBuf, index: 0, length: keyBuf.Length);

            if (!isStrix4Zone) // per key
            {
                for (int ledIndex = 0; ledIndex < packetMap.Count(); ledIndex++)
                {
                    ushort offset = (ushort)(3 * packetMap[ledIndex]);
                    byte zone = isStrixNumpad ? packetZoneNumpad[ledIndex] : packetZone[ledIndex];

                    keyBuf[offset] = color[zone].R;
                    keyBuf[offset + 1] = color[zone].G;
                    keyBuf[offset + 2] = color[zone].B;
                }

                for (int i = 0; i < keySet; i += ledsPerPacket)
                {
                    byte ledsRemaining = (byte)(keySet - i);

                    if (ledsRemaining < ledsPerPacket)
                    {
                        buffer[7] = ledsRemaining;
                    }

                    buffer[6] = (byte)i;
                    Buffer.BlockCopy(src: keyBuf, srcOffset: 3 * i, dst: buffer, dstOffset: 9, count: 3 * buffer[7]);
                    AsusHid.WriteAura(data: buffer);
                }
            }

            buffer[4] = 0x04;
            buffer[5] = 0x00;
            buffer[6] = 0x00;
            buffer[7] = 0x00;

            if (isStrix4Zone) { // per zone
                var leds_4_zone = packet4Zone.Count();
                for (int ledIndex = 0; ledIndex < leds_4_zone; ledIndex++)
                {
                    byte zone = packet4Zone[ledIndex];
                    keyBuf[ledIndex * 3] = color[zone].R;
                    keyBuf[ledIndex * 3 + 1] = color[zone].G;
                    keyBuf[ledIndex * 3 + 2] = color[zone].B;
                }
                Buffer.BlockCopy(src: keyBuf, srcOffset: 0, dst: buffer, dstOffset: 9, count: 3 * leds_4_zone);
                AsusHid.WriteAura(data: buffer);
                return;
            }

            Buffer.BlockCopy(src: keyBuf, srcOffset: 3 * keySet, dst: buffer, dstOffset: 9, count: 3 * (ledCount - keySet));
            AsusHid.WriteAura(data: buffer);
        }


        public static void ApplyColor(Color color, bool init = false)
        {

            if (isACPI)
            {
                Program.acpi.TUFKeyboardRGB(mode: 0, color: color, speed: 0, log: null);
                return;
            }

            if (isStrix && !isOldHeatmap)
            {
                ApplyDirect(color: Enumerable.Repeat(element: color, count: AURA_ZONES).ToArray(), init: init);
                return;
            }

            else
            {
                AsusHid.WriteAura(data: AuraMessage(mode: 0, color: color, color2: color, speed: 0));
                AsusHid.WriteAura(data: MESSAGE_SET);
            }

        }

        public static void ApplyAura()
        {

            Mode = (AuraMode)AppConfig.Get(name: "aura_mode");
            Speed = (AuraSpeed)AppConfig.Get(name: "aura_speed");
            SetColor(colorCode: AppConfig.Get(name: "aura_color"));
            SetColor2(colorCode: AppConfig.Get(name: "aura_color2"));

            timer.Enabled = false;

            if (Mode == AuraMode.HEATMAP)
            {
                CustomRGB.ApplyHeatmap(init: true);
                timer.Enabled = true;
                timer.Interval = 2000;
                return;
            }

            if (Mode == AuraMode.AMBIENT)
            {
                CustomRGB.ApplyAmbient(init: true);
                timer.Enabled = true;
                timer.Interval = 100;
                return;
            }

            if (Mode == AuraMode.GPUMODE)
            {
                CustomRGB.ApplyGPUColor();
                return;
            }

            int _speed = (Speed == AuraSpeed.Normal) ? 0xeb : (Speed == AuraSpeed.Fast) ? 0xf5 : 0xe1;

            AsusHid.Write(dataList: new List<byte[]> { AuraMessage(mode: Mode, color: Color1, color2: Color2, speed: _speed, mono: isSingleColor), MESSAGE_APPLY, MESSAGE_SET });

            if (isACPI)
                Program.acpi.TUFKeyboardRGB(mode: Mode, color: Color1, speed: _speed);

        }


        public static class CustomRGB
        {

            public static void ApplyGPUColor()
            {
                if ((AuraMode)AppConfig.Get(name: "aura_mode") != AuraMode.GPUMODE) return;

                switch (GPUModeControl.gpuMode)
                {
                    case AsusACPI.GPUModeUltimate:
                        ApplyColor(color: Color.Red, init: true);
                        break;
                    case AsusACPI.GPUModeEco:
                        ApplyColor(color: Color.Green, init: true);
                        break;
                    default:
                        ApplyColor(color: Color.Yellow, init: true);
                        break;
                }
            }

            public static void ApplyHeatmap(bool init = false)
            {
                float cpuTemp = (float)HardwareControl.GetCPUTemp();
                int freeze = 20, cold = 40, warm = 65, hot = 90;
                Color color;

                //Debug.WriteLine(cpuTemp);

                if (cpuTemp < cold)
                {
	                color = ColorUtils.GetWeightedAverage(color1: Color.Blue, color2: Color.Green, weight: ((float)cpuTemp - freeze) / (cold - freeze));
                }
                else if (cpuTemp < warm)
                {
	                color = ColorUtils.GetWeightedAverage(color1: Color.Green, color2: Color.Yellow, weight: ((float)cpuTemp - cold) / (warm - cold));
                }
                else if (cpuTemp < hot)
                {
	                color = ColorUtils.GetWeightedAverage(color1: Color.Yellow, color2: Color.Red, weight: ((float)cpuTemp - warm) / (hot - warm));
                }
                else
                {
	                color = Color.Red;
                }

                ApplyColor(color: color, init: init);
            }



            public static void ApplyAmbient(bool init = false)
            {
                var bound = Screen.GetBounds(pt: Point.Empty);
                bound.Y += bound.Height / 3;
                bound.Height -= (int)Math.Round(a: bound.Height * (0.33f + 0.022f)); // cut 1/3 of the top screen + windows panel

                Bitmap screen_low  = AmbientData.CamptureScreen(rec: bound, out_w: 512, out_h: 288);   //quality decreases greatly if it is less 512 ;
                Bitmap screeb_pxl = AmbientData.ResizeImage(image: screen_low, width: 4, height: 2);     // 4x2 zone. top for keyboard and bot for lightbar;

                int zones = AURA_ZONES;

                if (isStrix) // laptop with lightbar
                {
                    var mid_left = ColorUtils.GetMidColor(color1: screeb_pxl.GetPixel(x: 0, y: 1), color2: screeb_pxl.GetPixel(x: 1, y: 1));
                    var mid_right = ColorUtils.GetMidColor(color1: screeb_pxl.GetPixel(x: 2, y: 1), color2: screeb_pxl.GetPixel(x: 3, y: 1));

                    AmbientData.Colors[4].RGB = ColorUtils.HSV.UpSaturation(rgb: screeb_pxl.GetPixel(x: 1, y: 1)); // left bck
                    AmbientData.Colors[5].RGB = ColorUtils.HSV.UpSaturation(rgb: mid_left);  // center left
                    AmbientData.Colors[6].RGB = ColorUtils.HSV.UpSaturation(rgb: mid_right); // center right
                    AmbientData.Colors[7].RGB = ColorUtils.HSV.UpSaturation(rgb: screeb_pxl.GetPixel(x: 3, y: 1)); // right bck

                    for (int i = 0; i < 4; i++) // keyboard
                    {
	                    AmbientData.Colors[i].RGB = ColorUtils.HSV.UpSaturation(rgb: screeb_pxl.GetPixel(x: i, y: 0));
                    }
                }
                else
                {
                    zones = 1;
                    AmbientData.Colors[0].RGB = ColorUtils.HSV.UpSaturation(rgb: ColorUtils.GetDominantColor(bmp: screeb_pxl), increse: (float)0.3);
                }

                //screen_low.Save("big.jpg", ImageFormat.Jpeg);
                //screeb_pxl.Save("small.jpg", ImageFormat.Jpeg);

                screen_low.Dispose();
                screeb_pxl.Dispose();

                bool is_fresh = false;

                for (int i = 0; i < zones; i++)
                {
                    if (AmbientData.result[i].ToArgb() != AmbientData.Colors[i].RGB.ToArgb())
                    {
	                    is_fresh = true;
                    }

                    AmbientData.result[i] = AmbientData.Colors[i].RGB;
                }

                if (is_fresh)
                {
                    if (isStrix) ApplyDirect(color: AmbientData.result, init: init);
                    else ApplyColor(color: AmbientData.result[0], init: init);
                }

            }

            static class AmbientData
            {

                public enum StretchMode
                {
                    STRETCH_ANDSCANS = 1,
                    STRETCH_ORSCANS = 2,
                    STRETCH_DELETESCANS = 3,
                    STRETCH_HALFTONE = 4,
                }

                [DllImport(dllName: "user32.dll")]
                private static extern IntPtr GetDesktopWindow();

                [DllImport(dllName: "user32.dll")]
                private static extern IntPtr GetWindowDC(IntPtr hWnd);

                [DllImport(dllName: "gdi32.dll")]
                private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

                [DllImport(dllName: "gdi32.dll")]
                private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

                [DllImport(dllName: "gdi32.dll")]
                private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

                [DllImport(dllName: "user32.dll")]
                private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

                [DllImport(dllName: "gdi32.dll")]
                private static extern bool DeleteDC(IntPtr hdc);

                [DllImport(dllName: "gdi32.dll")]
                private static extern bool DeleteObject(IntPtr hObject);

                [DllImport(dllName: "gdi32.dll")]
                private static extern bool StretchBlt(IntPtr hdcDest, int nXOriginDest, int nYOriginDest,
                int nWidthDest, int nHeightDest,
                IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc, Int32 dwRop);

                [DllImport(dllName: "gdi32.dll")]
                static extern bool SetStretchBltMode(IntPtr hdc, StretchMode iStretchMode);

                /// <summary>
                /// Captures a screenshot. 
                /// </summary>
                public static Bitmap CamptureScreen(Rectangle rec, int out_w, int out_h)
                {
                    IntPtr desktop = GetDesktopWindow();
                    IntPtr hdc = GetWindowDC(hWnd: desktop);
                    IntPtr hdcMem = CreateCompatibleDC(hDC: hdc);

                    IntPtr hBitmap = CreateCompatibleBitmap(hDC: hdc, nWidth: out_w, nHeight: out_h);
                    IntPtr hOld = SelectObject(hDC: hdcMem, hObject: hBitmap);
                    SetStretchBltMode(hdc: hdcMem, iStretchMode: StretchMode.STRETCH_DELETESCANS);
                    StretchBlt(hdcDest: hdcMem, nXOriginDest: 0, nYOriginDest: 0, nWidthDest: out_w, nHeightDest: out_h, hdcSrc: hdc, nXOriginSrc: rec.X, nYOriginSrc: rec.Y, nWidthSrc: rec.Width, nHeightSrc: rec.Height, dwRop: 0x00CC0020);
                    SelectObject(hDC: hdcMem, hObject: hOld);

                    DeleteDC(hdc: hdcMem);
                    ReleaseDC(hWnd: desktop, hDC: hdc);
                    var result = Image.FromHbitmap(hbitmap: hBitmap, hpalette: IntPtr.Zero);
                    DeleteObject(hObject: hBitmap);
                    return result;
                }

                public static Bitmap ResizeImage(Image image, int width, int height)
                {
                    var destRect = new Rectangle(x: 0, y: 0, width: width, height: height);
                    var destImage = new Bitmap(width: width, height: height);

                    destImage.SetResolution(xDpi: image.HorizontalResolution, yDpi: image.VerticalResolution);

                    using (var graphics = Graphics.FromImage(image: destImage))
                    {
                        graphics.CompositingMode = CompositingMode.SourceCopy;
                        graphics.CompositingQuality = CompositingQuality.HighQuality;
                        graphics.InterpolationMode = InterpolationMode.Bicubic;
                        graphics.SmoothingMode = SmoothingMode.None;
                        graphics.PixelOffsetMode = PixelOffsetMode.None;

                        using (var wrapMode = new ImageAttributes())
                        {
                            wrapMode.SetWrapMode(mode: WrapMode.TileFlipXY);
                            graphics.DrawImage(image: image, destRect: destRect, srcX: 0, srcY: 0, srcWidth: image.Width, srcHeight: image.Height, srcUnit: GraphicsUnit.Pixel, imageAttr: wrapMode);
                        }
                    }

                    return destImage;
                }

                static public Color[] result = new Color[AURA_ZONES];
                static public ColorUtils.SmoothColor[] Colors = Enumerable.Repeat(element: 0, count: AURA_ZONES).
                    Select(selector: h => new ColorUtils.SmoothColor()).ToArray();

                public static Color GetMostUsedColor(Bitmap bitMap)
                {
                    var colorIncidence = new Dictionary<int, int>();
                    for (var x = 0; x < bitMap.Size.Width; x++)
                        for (var y = 0; y < bitMap.Size.Height; y++)
                        {
                            var pixelColor = bitMap.GetPixel(x: x, y: y).ToArgb();
                            if (colorIncidence.Keys.Contains(item: pixelColor))
                            {
	                            colorIncidence[key: pixelColor]++;
                            }
                            else
                                colorIncidence.Add(key: pixelColor, value: 1);
                        }
                    return Color.FromArgb(argb: colorIncidence.OrderByDescending(keySelector: x => x.Value).ToDictionary(keySelector: x => x.Key, elementSelector: x => x.Value).First().Key);
                }
            }

        }

    }

}