using TextMateSharp.Grammars;

namespace Consolonia.Editor.ViewModels
{
    public class ThemeViewModel(ThemeName themeName)
    {
        public ThemeName ThemeName { get; } = themeName;

        public string DisplayName => ThemeName.ToString();
    }
}