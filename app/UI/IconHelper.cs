using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GHelper.UI
{
    public class IconHelper
    {

        [DllImport(dllName: "user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_SETICON = 0x80u;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;


        public static void SetIcon(Form form, Bitmap icon)
        {
            try
            {
                SendMessage(hWnd: form.Handle, Msg: WM_SETICON, wParam: ICON_BIG, lParam: Icon.ExtractAssociatedIcon(filePath: Application.ExecutablePath)!.Handle);
                SendMessage(hWnd: form.Handle, Msg: WM_SETICON, wParam: ICON_SMALL, lParam: icon.GetHicon());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(message: $"Error setting icon {ex.Message}");
            }
        }

    }
}
