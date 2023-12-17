﻿using System.Runtime.InteropServices;

public sealed class KeyboardHook : IDisposable
{
    // Registers a hot key with Windows.
    [DllImport(dllName: "user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    // Unregisters the hot key with Windows.
    [DllImport(dllName: "user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport(dllName: "user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport(dllName: "user32.dll", SetLastError = true)]
    public static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, IntPtr extraInfo);

    public const int KEYEVENTF_EXTENDEDKEY = 1;
    public const int KEYEVENTF_KEYUP = 2;

    private const byte VK_LWIN = 0x5B;
    private const byte VK_LCONTROL = 0xA2;

    public static void KeyPress(Keys key)
    {
        keybd_event(virtualKey: (byte)key, scanCode: 0, flags: KEYEVENTF_EXTENDEDKEY, extraInfo: IntPtr.Zero);
    }

    public static void KeyKeyPress(Keys key, Keys key2)
    {
        keybd_event(virtualKey: (byte)key, scanCode: 0, flags: KEYEVENTF_EXTENDEDKEY, extraInfo: IntPtr.Zero);
        keybd_event(virtualKey: (byte)key2, scanCode: 0, flags: KEYEVENTF_EXTENDEDKEY, extraInfo: IntPtr.Zero);
        keybd_event(virtualKey: (byte)key2, scanCode: 0, flags: KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, extraInfo: IntPtr.Zero);
        keybd_event(virtualKey: (byte)key, scanCode: 0, flags: KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, extraInfo: IntPtr.Zero);
    }

    public static void KeyKeyKeyPress(Keys key, Keys key2, Keys key3, int sleep = 0)
    {
        keybd_event(virtualKey: (byte)key, scanCode: 0, flags: KEYEVENTF_EXTENDEDKEY, extraInfo: IntPtr.Zero);
        keybd_event(virtualKey: (byte)key2, scanCode: 0, flags: KEYEVENTF_EXTENDEDKEY, extraInfo: IntPtr.Zero);
        keybd_event(virtualKey: (byte)key3, scanCode: 0, flags: KEYEVENTF_EXTENDEDKEY, extraInfo: IntPtr.Zero);

        if (sleep > 0)
        {
            Thread.Sleep(millisecondsTimeout: sleep);
        }

        keybd_event(virtualKey: (byte)key3, scanCode: 0, flags: KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, extraInfo: IntPtr.Zero);
        keybd_event(virtualKey: (byte)key2, scanCode: 0, flags: KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, extraInfo: IntPtr.Zero);
        keybd_event(virtualKey: (byte)key, scanCode: 0, flags: KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, extraInfo: IntPtr.Zero);
    }

    /// <summary>
    /// Represents the window that is used internally to get the messages.
    /// </summary>
    private class Window : NativeWindow, IDisposable
    {
        private static int WM_HOTKEY = 0x0312;
        public static Keys? fakeKey;

        public Window()
        {
            // create the handle for the window.
            this.CreateHandle(cp: new CreateParams());
        }

        /// <summary>
        /// Overridden to get the notifications.
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(m: ref m);

            // check if we got a hot key pressed.
            if (m.Msg == WM_HOTKEY)
            {
                // get the keys.
                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                ModifierKeys modifier = (ModifierKeys)((int)m.LParam & 0xFFFF);

                // invoke the event to notify the parent.
                if (KeyPressed != null)
                    KeyPressed(sender: this, e: new KeyPressedEventArgs(modifier: modifier, key: key));
            }
        }

        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        #region IDisposable Members

        public void Dispose()
        {
            this.DestroyHandle();
        }

        #endregion
    }

    private Window _window = new Window();
    private int _currentId;

    public KeyboardHook()
    {
        // register the event of the inner native window.
        _window.KeyPressed += delegate (object sender, KeyPressedEventArgs args)
        {
            if (KeyPressed != null)
                KeyPressed(sender: this, e: args);
        };
    }

    /// <summary>
    /// Registers a hot key in the system.
    /// </summary>
    /// <param name="modifier">The modifiers that are associated with the hot key.</param>
    /// <param name="key">The key itself that is associated with the hot key.</param>
    public void RegisterHotKey(ModifierKeys modifier, Keys key)
    {
        // increment the counter.
        _currentId = _currentId + 1;

        // register the hot key.
        if (!RegisterHotKey(hWnd: _window.Handle, id: _currentId, fsModifiers: (uint)modifier, vk: (uint)key))
            Logger.WriteLine(logMessage: "Couldn’t register " + key);
    }

    /// <summary>
    /// A hot key has been pressed.
    /// </summary>
    public event EventHandler<KeyPressedEventArgs> KeyPressed;

    #region IDisposable Members

    public void UnregisterAll()
    {
        // unregister all the registered hot keys.
        for (int i = _currentId; i > 0; i--)
        {
            UnregisterHotKey(hWnd: _window.Handle, id: i);
        }
    }

    public void Dispose()
    {
        UnregisterAll();
        // dispose the inner native window.
        _window.Dispose();
    }

    #endregion
}

/// <summary>
/// Event Args for the event that is fired after the hot key has been pressed.
/// </summary>
public class KeyPressedEventArgs : EventArgs
{
    private ModifierKeys _modifier;
    private Keys _key;

    internal KeyPressedEventArgs(ModifierKeys modifier, Keys key)
    {
        _modifier = modifier;
        _key = key;
    }

    public ModifierKeys Modifier
    {
        get { return _modifier; }
    }

    public Keys Key
    {
        get { return _key; }
    }
}

/// <summary>
/// The enumeration of possible modifiers.
/// </summary>
[Flags]
public enum ModifierKeys : uint
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}