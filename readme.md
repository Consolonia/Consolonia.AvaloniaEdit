![logo](https://raw.githubusercontent.com/tomlm/ConsoloniaContent/main/Logo.png)

# Consolonia.AvaloniaEdit
This package contains theme resources for using **AvaloniaEdit** in [Consolonia](https://github.com/jinek/consolonia) 
applications.

![avalonia editor](https://raw.githubusercontent.com/tomlm/ConsoloniaContent/main/AvaloniaEdit.gif)

# Usage
To enable AvaloniaEdit in your application:
1. Add **Avalonia.AvaloniaEdit** NuGet package
1. Add **Consolonia.AvaloniaEdit** NuGet package
1. Add `AvaloniaEditTheme` to your application styles, with `xmlns:console="https://github.com/jinek/consolonia"` declared on your application.

```xaml
    <Application.Styles>
        <console:ModernTheme />
        <console:AvaloniaEditTheme />
    </Application.Styles>

```

