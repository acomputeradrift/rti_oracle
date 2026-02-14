using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OracleByFPCLtd;

internal static class WindowIconLoader
{
    public static void TryApply(Window window)
    {
        try
        {
            var packUri = new Uri("pack://application:,,,/Resources/AppIcon.ico", UriKind.Absolute);
            window.Icon = BitmapFrame.Create(packUri);
            return;
        }
        catch (Exception)
        {
            // Cosmetic fallback only: icon load must never prevent window startup.
        }

        try
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "Resources", "AppIcon.ico");
            if (File.Exists(filePath))
            {
                window.Icon = BitmapFrame.Create(new Uri(filePath, UriKind.Absolute));
            }
        }
        catch (Exception)
        {
            // Cosmetic fallback only: leave icon unset if both strategies fail.
        }
    }
}
