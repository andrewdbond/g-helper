namespace GHelper.Peripherals.Mouse.Models
{
    //P711
    public class GladiusIIIAimpoint : AsusMouse
    {
        public GladiusIIIAimpoint() : base(vendorId: 0x0B05, productId: 0x1A70, path: "mi_00", wireless: true)
        {
        }

        protected GladiusIIIAimpoint(ushort vendorId, bool wireless) : base(vendorId: 0x0B05, productId: vendorId, path: "mi_00", wireless: wireless)
        {
        }

        public override int DPIProfileCount()
        {
            return 4;
        }

        public override string GetDisplayName()
        {
            return "ROG Gladius III Aimpoint (Wireless)";
        }


        public override PollingRate[] SupportedPollingrates()
        {
            return new PollingRate[] {
                PollingRate.PR125Hz,
                PollingRate.PR250Hz,
                PollingRate.PR500Hz,
                PollingRate.PR1000Hz
            };
        }

        public override int ProfileCount()
        {
            return 5;
        }
        public override int MaxDPI()
        {
            return 36_000;
        }

        public override bool HasXYDPI()
        {
            return true;
        }

        public override bool HasDebounceSetting()
        {
            return true;
        }

        public override bool HasLiftOffSetting()
        {
            return true;
        }

        public override bool HasRGB()
        {
            return true;
        }

        public override LightingZone[] SupportedLightingZones()
        {
            return new LightingZone[] { LightingZone.Logo, LightingZone.Scrollwheel, LightingZone.Underglow };
        }

        public override bool HasAutoPowerOff()
        {
            return true;
        }

        public override bool HasAngleSnapping()
        {
            return true;
        }

        public override bool HasAngleTuning()
        {
            return true;
        }

        public override bool HasLowBatteryWarning()
        {
            return true;
        }

        public override bool HasDPIColors()
        {
            return true;
        }
    }

    public class GladiusIIIAimpointWired : GladiusIIIAimpoint
    {
        public GladiusIIIAimpointWired() : base(vendorId: 0x1A72, wireless: false)
        {
        }

        public override string GetDisplayName()
        {
            return "ROG Gladius III Aimpoint (Wired)";
        }
    }
}
