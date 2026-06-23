using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace WinCalc;

public partial class App : Application
{
    public enum AppTheme { Light, Dark, System }
    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;

    private void App_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            SetTheme(AppTheme.System);
            var window = new MainWindow();
            window.Show();
            window.Activate();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup error:\n\n{ex.Message}\n\n{ex.StackTrace}",
                            "Calculator Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    public void SetTheme(AppTheme mode)
    {
        CurrentTheme = mode;
        bool dark = mode == AppTheme.Dark ||
                   (mode == AppTheme.System && IsSystemDark());
        ApplyColors(dark);
    }

    private void ApplyColors(bool dark)
    {
        if (dark)
        {
            Set("CardBg",    "#242330");
            Set("HistBg",    "#1E1C2A");
            Set("BtnBg",     "#35334A");
            Set("BtnHover",  "#42405A");
            Set("BtnPress",  "#4E4B68");
            Set("OpBg",      "#2D2B3E");
            Set("OpHover",   "#3A3850");
            Set("Fg",        "#EEEDF4");
            Set("SubFg",     "#888AAA");
            Set("AccentBg",  "#3584E4");
            Set("AccentHov", "#2C74D0");
            Set("TitleBg",   "#1E1C2A");
            Set("WarnFg",    "#FF6B6B");
            Set("HistLine",  "#2E2C3E");
        }
        else
        {
            Set("CardBg",    "#FFFFFF");
            Set("HistBg",    "#F5F4F8");
            Set("BtnBg",     "#EFEFEF");
            Set("BtnHover",  "#E2E0EA");
            Set("BtnPress",  "#D5D3DE");
            Set("OpBg",      "#E4E2EC");
            Set("OpHover",   "#D8D5E4");
            Set("Fg",        "#1C1B1F");
            Set("SubFg",     "#888888");
            Set("AccentBg",  "#3584E4");
            Set("AccentHov", "#2C74D0");
            Set("TitleBg",   "#FAFAF8");
            Set("WarnFg",    "#CC3333");
            Set("HistLine",  "#EBEBEB");
        }
    }

    private void Set(string key, string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        Resources[key] = new SolidColorBrush(color);
    }

    public static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
        }
        catch { return false; }
    }
}
