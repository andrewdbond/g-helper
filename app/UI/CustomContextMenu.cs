using System.Runtime.InteropServices;

namespace GHelper.UI
{
    class CustomContextMenu : ContextMenuStrip
    {
        [DllImport(dllName: "dwmapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern long DwmSetWindowAttribute(nint hwnd,
                                                            DWMWINDOWATTRIBUTE attribute,
                                                            ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute,
                                                            uint cbAttribute);

        public CustomContextMenu()
        {
            var preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUNDSMALL;     //change as you want
            DwmSetWindowAttribute(hwnd: Handle,
                                  attribute: DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
                                  pvAttribute: ref preference,
                                  cbAttribute: sizeof(uint));
        }

        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_WINDOW_CORNER_PREFERENCE = 33
        }
        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWA_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3,
        }
    }

}