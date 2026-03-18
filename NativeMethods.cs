using System.Runtime.InteropServices;

namespace ClaudeUsageTray;

/// <summary>P/Invoke declarations for Win32 icon management.</summary>
internal static class NativeMethods
{
    /// <summary>
    /// Destroys an icon and frees any memory the icon occupied.
    /// Must be called for every HICON obtained via Bitmap.GetHicon().
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(IntPtr hIcon);
}
