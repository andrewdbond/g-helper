//
// This is a optimised/simplified version of Ryzen System Management Unit from https://github.com/JamesCJ60/Universal-x86-Tuning-Utility
// I do not take credit for the full functionality of the code (c)
//


namespace Ryzen
{
    internal class SendCommand
    {

        //RAVEN - 0
        //PICASSO - 1
        //DALI - 2
        //RENOIR/LUCIENNE - 3
        //MATISSE - 4
        //VANGOGH - 5
        //VERMEER - 6
        //CEZANNE/BARCELO - 7
        //REMBRANDT - 8
        //PHEONIX - 9
        //RAPHAEL/DRAGON RANGE - 10

        public static Smu RyzenAccess = new Smu(EnableDebug: false);
        public static int FAMID = RyzenControl.FAMID;


        //STAMP Limit
        public static Smu.Status? set_stapm_limit(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;
            Smu.Status? result = null;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    result = RyzenAccess.SendMp1(message: 0x1a, arguments: ref Args);
                    break;
                case 3:
                case 5:
                case 7:
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendMp1(message: 0x14, arguments: ref Args);
                    result = RyzenAccess.SendPsmu(message: 0x31, arguments: ref Args);
                    break;
                default:
                    break;
            }

            RyzenAccess.Deinitialize();
            return result;
            
        }

        //STAMP2 Limit
        public static Smu.Status? set_stapm2_limit(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;
            Smu.Status? result = null;

            switch (FAMID)
            {
                case 3:
                case 5:
                case 7:
                case 8:
                case 9:
                case 11:
                    result = RyzenAccess.SendPsmu(message: 0x31, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
            return result;
        }

        //Fast Limit
        public static Smu.Status? set_fast_limit(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;
            Smu.Status? result = null;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    result = RyzenAccess.SendMp1(message: 0x1b, arguments: ref Args);
                    break;
                case 3:
                case 5:
                case 7:
                case 8:
                case 9:
                case 11:
                    result = RyzenAccess.SendMp1(message: 0x15, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
            return result;
        }

        //Slow Limit
        public static Smu.Status? set_slow_limit(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;
            Smu.Status? result = null;
            
            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    result = RyzenAccess.SendMp1(message: 0x1c, arguments: ref Args);
                    break;
                case 3:
                case 5:
                case 7:
                case 8:
                case 9:
                case 11:
                    result = RyzenAccess.SendMp1(message: 0x16, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
            return result;
        }

        //Slow time
        public static void set_slow_time(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x1d, arguments: ref Args);
                    break;
                case 3:
                case 5:
                case 7:
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendMp1(message: 0x17, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //STAMP Time
        public static void set_stapm_time(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x1e, arguments: ref Args);
                    break;
                case 3:
                case 5:
                case 7:
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendMp1(message: 0x18, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //TCTL Temp Limit
        public static Smu.Status? set_tctl_temp(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            Smu.Status? result = null;

            switch (FAMID)
            {
                case -1:
                    result = RyzenAccess.SendPsmu(message: 0x68, arguments: ref Args);
                    break;
                case 0:
                case 1:
                case 2:
                    result = RyzenAccess.SendMp1(message: 0x1f, arguments: ref Args);
                    break;
                case 3:
                case 5:
                case 7:
                case 8:
                case 9:
                case 11:
                    result = RyzenAccess.SendMp1(message: 0x19, arguments: ref Args);
                    break;
                case 4:
                case 6:
                    RyzenAccess.SendMp1(message: 0x23, arguments: ref Args);
                    result = RyzenAccess.SendPsmu(message: 0x56, arguments: ref Args);
                    break;
                case 10:
                    result = RyzenAccess.SendPsmu(message: 0x59, arguments: ref Args);
                    break;
                default:
                    break;
            }

            RyzenAccess.Deinitialize();
            return result;
        }

        //cHTC Temp Limit
        public static void set_cHTC_temp(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendPsmu(message: 0x56, arguments: ref Args);
                    break;
                case 3:
                case 5:
                case 7:
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendPsmu(message: 0x37, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //Skin Temp limit
        public static Smu.Status? set_apu_skin_temp_limit(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            Smu.Status? result = null;

            switch (FAMID)
            {
                case 5:
                case 8:
                case 9:
                case 11:
                    result = RyzenAccess.SendMp1(message: 0x33, arguments: ref Args);
                    break;
                case 3:
                case 7:
                    result = RyzenAccess.SendMp1(message: 0x38, arguments: ref Args);
                    break;
                default:
                    break;
            }

            RyzenAccess.Deinitialize();

            return result;
        }

        //VRM Current
        public static void set_vrm_current(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x20, arguments: ref Args);
                    break;
                case 3:
                case 5:
                case 7:
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendMp1(message: 0x1a, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //VRM SoC Current
        public static void set_vrmsoc_current(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x21, arguments: ref Args);
                    break;
                case 3:
                case 5:
                case 7:
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendMp1(message: 0x1b, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //VRM GFX Current
        public static void set_vrmgfx_current(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 5:
                    RyzenAccess.SendMp1(message: 0x1c, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //VRM CVIP Current
        public static void set_vrmcvip_current(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 5:
                    RyzenAccess.SendMp1(message: 0x1d, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //VRM Max Current
        public static void set_vrmmax_current(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x22, arguments: ref Args);
                    break;
                case 5:
                    RyzenAccess.SendMp1(message: 0x1e, arguments: ref Args);
                    break;
                case 3:
                case 7:
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendMp1(message: 0x1c, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //VRM GFX Max Current
        public static void set_vrmgfxmax_current(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 5:
                    RyzenAccess.SendMp1(message: 0x1f, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //VRM SoC Max Current
        public static void set_vrmsocmax_current(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x23, arguments: ref Args);
                    break;
                case 3:
                case 5:
                case 7:
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendMp1(message: 0x1d, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //GFX Clock Max
        public static void set_max_gfxclk_freq(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x46, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //GFX Clock Min
        public static void set_min_gfxclk_freq(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x47, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //SoC Clock Max
        public static void set_max_socclk_freq(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x48, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //SoC Clock Min
        public static void set_min_socclk_freq(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x49, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //FCLK Clock Max
        public static void set_max_fclk_freq(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x4a, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //FCLK Clock Min
        public static void set_min_fclk_freq(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x4b, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //VCN Clock Max
        public static void set_max_vcn_freq(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x4c, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //VCN Clock Min
        public static void set_min_vcn_freq(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x4d, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //LCLK Clock Max
        public static void set_max_lclk(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x4e, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //LCLK Clock Min
        public static void set_min_lclk(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x4f, arguments: ref Args);
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //Prochot Ramp
        public static void set_prochot_deassertion_ramp(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x26, arguments: ref Args);
                    break;
                case 5:
                    RyzenAccess.SendMp1(message: 0x22, arguments: ref Args);
                    break;
                case 3:
                case 7:
                    RyzenAccess.SendMp1(message: 0x20, arguments: ref Args);
                    break;
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendMp1(message: 0x1f, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //GFX Clock
        public static void set_gfx_clk(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 3:
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendPsmu(message: 0x89, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //dGPU Skin Temp
        public static void set_dGPU_skin(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 3:
                case 7:
                    RyzenAccess.SendMp1(message: 0x37, arguments: ref Args);
                    break;
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendMp1(message: 0x32, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //Power Saving
        public static void set_power_saving(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x19, arguments: ref Args);
                    break;
                case 3:
                case 5:
                case 7:
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendMp1(message: 0x12, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //Max Performance
        public static void set_max_performance(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendMp1(message: 0x18, arguments: ref Args);
                    break;
                case 3:
                case 5:
                case 7:
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendMp1(message: 0x11, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //Set All Core OC
        public static void set_oc_clk(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case -1:
                    RyzenAccess.SendPsmu(message: 0x6c, arguments: ref Args);
                    break;
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendPsmu(message: 0x7d, arguments: ref Args);
                    break;
                case 3:
                case 7:
                    RyzenAccess.SendMp1(message: 0x31, arguments: ref Args);
                    RyzenAccess.SendPsmu(message: 0x19, arguments: ref Args);
                    break;
                case 4:
                case 6:
                    RyzenAccess.SendMp1(message: 0x26, arguments: ref Args);
                    RyzenAccess.SendPsmu(message: 0x5c, arguments: ref Args);
                    break;
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendPsmu(message: 0x19, arguments: ref Args);
                    break;
                case 10:
                    RyzenAccess.SendPsmu(message: 0x5F, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //Set Per Core OC
        public static void set_per_core_oc_clk(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case -1:
                    RyzenAccess.SendPsmu(message: 0x6d, arguments: ref Args);
                    break;
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendPsmu(message: 0x7E, arguments: ref Args);
                    break;
                case 3:
                case 7:
                    RyzenAccess.SendMp1(message: 0x32, arguments: ref Args);
                    RyzenAccess.SendPsmu(message: 0x1a, arguments: ref Args);
                    break;
                case 4:
                case 6:
                    RyzenAccess.SendMp1(message: 0x27, arguments: ref Args);
                    RyzenAccess.SendPsmu(message: 0x5d, arguments: ref Args);
                    break;
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendPsmu(message: 0x1a, arguments: ref Args);
                    break;
                case 10:
                    RyzenAccess.SendPsmu(message: 0x60, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //Set VID
        public static void set_oc_volt(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case -1:
                    RyzenAccess.SendPsmu(message: 0x6e, arguments: ref Args);
                    break;
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendPsmu(message: 0x7f, arguments: ref Args);
                    break;
                case 3:
                case 7:
                    RyzenAccess.SendMp1(message: 0x33, arguments: ref Args);
                    RyzenAccess.SendPsmu(message: 0x1b, arguments: ref Args);
                    break;
                case 4:
                case 6:
                    RyzenAccess.SendMp1(message: 0x28, arguments: ref Args);
                    RyzenAccess.SendPsmu(message: 0x61, arguments: ref Args);
                    break;
                case 10:
                    RyzenAccess.SendPsmu(message: 0x61, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //Set All Core Curve Optimiser
        public static Smu.Status? set_coall(int value)
        {

            uint uvalue = Convert.ToUInt32(value: 0x100000 - (uint)(-1 * value));

            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = uvalue;

            Smu.Status? result = null;

            switch (FAMID)
            {
                case 3:
                case 7:
                    RyzenAccess.SendMp1(message: 0x55, arguments: ref Args);
                    result = RyzenAccess.SendPsmu(message: 0xB1, arguments: ref Args);
                    break;
                case 4:
                case 6:
                    RyzenAccess.SendMp1(message: 0x36, arguments: ref Args);
                    result = RyzenAccess.SendPsmu(message: 0xB, arguments: ref Args);
                    break;
                case 5:
                case 8:
                case 9:
                case 11:
                    result = RyzenAccess.SendPsmu(message: 0x5D, arguments: ref Args);
                    break;
                case 10:
                    result = RyzenAccess.SendPsmu(message: 0x7, arguments: ref Args);
                    break;
                default:
                    break;
            }

            RyzenAccess.Deinitialize();
            return result;

        }

        //Set Per Core Curve Optimiser
        public static void set_coper(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case 3:
                case 7:
                    RyzenAccess.SendMp1(message: 0x54, arguments: ref Args);
                    break;
                case 4:
                case 6:
                    RyzenAccess.SendMp1(message: 0x35, arguments: ref Args);
                    break;
                case 5:
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendMp1(message: 0x4b, arguments: ref Args);
                    break;
                case 10:
                    RyzenAccess.SendPsmu(message: 0x6, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //Set iGPU Curve Optimiser
        public static Smu.Status? set_cogfx(int value)
        {

            uint uvalue = Convert.ToUInt32(value: 0x100000 - (uint)(-1 * value));

            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = uvalue;

            Smu.Status? result = null;

            switch (FAMID)
            {
                case 3:
                case 7:
                    RyzenAccess.SendMp1(message: 0x64, arguments: ref Args);
                    result = RyzenAccess.SendPsmu(message: 0x57, arguments: ref Args);
                    break;
                case 5:
                case 8:
                case 9:
                case 11:
                    result = RyzenAccess.SendPsmu(message: 0xb7, arguments: ref Args);
                    break;
                default:
                    break;
            }

            RyzenAccess.Deinitialize();
            return result;
        }

        //Disable OC
        public static void set_disable_oc()
        {
            uint value = 0x0;
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case -1:
                    RyzenAccess.SendMp1(message: 0x24, arguments: ref Args);
                    break;
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendPsmu(message: 0x6A, arguments: ref Args);
                    break;
                case 3:
                case 7:
                    RyzenAccess.SendMp1(message: 0x30, arguments: ref Args);
                    RyzenAccess.SendPsmu(message: 0x1d, arguments: ref Args);
                    break;
                case 4:
                case 6:
                    RyzenAccess.SendMp1(message: 0x25, arguments: ref Args);
                    RyzenAccess.SendPsmu(message: 0x5b, arguments: ref Args);
                    break;
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendPsmu(message: 0x18, arguments: ref Args);
                    break;
                case 10:
                    RyzenAccess.SendPsmu(message: 0x5E, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //Enable OC
        public static void set_enable_oc()
        {
            uint value = 0x0;
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case -1:
                    RyzenAccess.SendMp1(message: 0x23, arguments: ref Args);
                    break;
                case 0:
                case 1:
                case 2:
                    RyzenAccess.SendPsmu(message: 0x69, arguments: ref Args);
                    break;
                case 3:
                case 7:
                    RyzenAccess.SendMp1(message: 0x2f, arguments: ref Args);
                    RyzenAccess.SendPsmu(message: 0x1d, arguments: ref Args);
                    break;
                case 4:
                case 6:
                    RyzenAccess.SendMp1(message: 0x24, arguments: ref Args);
                    RyzenAccess.SendPsmu(message: 0x5a, arguments: ref Args);
                    break;
                case 8:
                case 9:
                case 11:
                    RyzenAccess.SendPsmu(message: 0x17, arguments: ref Args);
                    break;
                case 10:
                    RyzenAccess.SendPsmu(message: 0x5D, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //Set PBO Scaler
        public static void set_scaler(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case -1:
                    RyzenAccess.SendPsmu(message: 0x6a, arguments: ref Args);
                    break;
                case 4:
                case 6:
                    RyzenAccess.SendPsmu(message: 0x58, arguments: ref Args);
                    RyzenAccess.SendMp1(message: 0x2F, arguments: ref Args);
                    break;
                case 10:
                    RyzenAccess.SendPsmu(message: 0x5b, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }


        //Set PPT
        public static void set_ppt(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case -1:
                    RyzenAccess.SendPsmu(message: 0x64, arguments: ref Args);
                    break;
                case 4:
                case 6:
                    RyzenAccess.SendPsmu(message: 0x53, arguments: ref Args);
                    RyzenAccess.SendMp1(message: 0x3D, arguments: ref Args);
                    break;
                case 10:
                    RyzenAccess.SendPsmu(message: 0x56, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }


        //Set TDC
        public static void set_tdc(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case -1:
                    RyzenAccess.SendPsmu(message: 0x65, arguments: ref Args);
                    break;
                case 4:
                case 6:
                    RyzenAccess.SendPsmu(message: 0x54, arguments: ref Args);
                    RyzenAccess.SendMp1(message: 0x3B, arguments: ref Args);
                    break;
                case 10:
                    RyzenAccess.SendPsmu(message: 0x57, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }

        //Set EDC
        public static void set_edc(uint value)
        {
            RyzenAccess.Initialize();
            uint[] Args = new uint[6];
            Args[0] = value;

            switch (FAMID)
            {
                case -1:
                    RyzenAccess.SendPsmu(message: 0x66, arguments: ref Args);
                    break;
                case 4:
                case 6:
                    RyzenAccess.SendPsmu(message: 0x55, arguments: ref Args);
                    RyzenAccess.SendMp1(message: 0x3c, arguments: ref Args);
                    break;
                case 10:
                    RyzenAccess.SendPsmu(message: 0x58, arguments: ref Args);
                    break;
                default:
                    break;
            }
            RyzenAccess.Deinitialize();
        }
    }
}
