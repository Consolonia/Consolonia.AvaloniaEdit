using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Consolonia.AvaloniaEdit
{
    /// <summary>
    ///     Provides the Consolonia styles for AvaloniaEdit controls.
    /// </summary>
    public class AvaloniaEditTheme : Styles
    {
        public AvaloniaEditTheme()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}