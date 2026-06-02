# WinCalc — GNOME Calculator for Windows

A fast, native WPF calculator that mirrors the GNOME Calculator look.
Built with C# + WPF (.NET 8). Tiny binary, instant startup.

## Requirements
- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build & Run

```
dotnet run
```

Or build a standalone exe:

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output: `bin/Release/net8.0-windows/win-x64/publish/WinCalc.exe`

## Features

| Feature | Detail |
|---------|--------|
| **Modes** | Basic, Advanced |
| **Theme** | Light / Dark / System Default (reads Windows registry) |
| **History** | Scrollable step-by-step history; click any row to restore result |
| **Keyboard** | Full keyboard input + Enter to evaluate, Esc to clear |
| **Operators** | +  −  ×  ÷  mod  %  ^  ( ) |
| **Functions** | sin cos tan ln log abs sqrt x! π e sin⁻¹ cos⁻¹ tan⁻¹ sinh cosh tanh sinh⁻¹ cosh⁻¹ tanh⁻¹ 2ˣ x³ x! ³√|
| **Extras** | ±  x⁻¹  x²  xʸ  copy result  clear history |

## Theme Switching

Click **≡** (hamburger) → choose Light / Dark / System Default.  
System Default reads `HKCU\...\Themes\Personalize\AppsUseLightTheme`.

## File Structure

```
WinCalc/
├── WinCalc.csproj         # .NET 8 WPF project
├── App.xaml               # Global styles (button templates, combo)
├── App.xaml.cs            # Theme switching logic
├── MainWindow.xaml        # UI layout
├── MainWindow.xaml.cs     # All interaction logic + button grid builder
├── Calculator.cs          # Expression evaluator (DataTable + preprocessing)
└── Themes/
    ├── Light.xaml         # Light palette
    └── Dark.xaml          # Dark palette
```
