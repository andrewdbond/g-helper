﻿namespace GHelper.Peripherals.Mouse.Models
{
    //P709_Wireless
    public class KerisWirelssAimpoint : AsusMouse
    {
        public KerisWirelssAimpoint() : base(vendorId: 0x0B05, productId: 0x1A68, path: "mi_00", wireless: true)
        {
        }

        protected KerisWirelssAimpoint(ushort vendorId, bool wireless) : base(vendorId: 0x0B05, productId: vendorId, path: "mi_00", wireless: wireless)
        {
        }

        public override int DPIProfileCount()
        {
            return 4;
        }

        public override string GetDisplayName()
        {
            return "ROG Keris Wireless Aimpoint (Wireless)";
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
            return new LightingZone[] { LightingZone.Logo };
        }

        public override bool IsLightingModeSupported(LightingMode lightingMode)
        {
            return lightingMode == LightingMode.Static
                || lightingMode == LightingMode.Breathing
                || lightingMode == LightingMode.ColorCycle
                || lightingMode == LightingMode.BatteryState
                || lightingMode == LightingMode.React;
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

    public class KerisWirelssAimpointWired : KerisWirelssAimpoint
    {
        public KerisWirelssAimpointWired() : base(vendorId: 0x1A66, wireless: false)
        {
        }

        public override string GetDisplayName()
        {
            return "ROG Keris Wireless Aimpoint (Wired)";
        }
    }
}
