using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Indentation.CSharp;
using AvaloniaEdit.TextMate;
using Consolonia.Editor.Resources;
using Consolonia.Editor.ViewModels;
using TextMateSharp.Grammars;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
namespace Consolonia.Editor
{
    public partial class MainWindow : Window
    {
        private readonly int _currentTheme = (int)ThemeName.VisualStudioDark;
        private readonly RegistryOptions _registryOptions;
        private readonly TextBlock _statusTextBlock;
        private readonly ComboBox _syntaxModeCombo;
        private readonly TextEditor _textEditor;
        private readonly TextMate.Installation _textMateInstallation;
        private FoldingManager? _foldingManager;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public MainWindow()
        {
            InitializeComponent();
            // this.AttachDevTools();

            _textEditor = this.FindControl<TextEditor>("Editor")!;
            _textEditor.TextArea.IndentationStrategy = new CSharpIndentationStrategy(_textEditor.Options);
            _textEditor.TextArea.Caret.PositionChanged += Caret_PositionChanged;
            _textEditor.TextArea.RightClickMovesCaret = true;

            _registryOptions = new RegistryOptions(
                (ThemeName)_currentTheme);

            _textMateInstallation = _textEditor.InstallTextMate(_registryOptions);

            _textMateInstallation.AppliedTheme += TextMateInstallationOnAppliedTheme;

            Language csharpLanguage = _registryOptions.GetLanguageByExtension(".cs");

            _syntaxModeCombo = this.FindControl<ComboBox>("SyntaxModeCombo")!;
            _syntaxModeCombo.ItemsSource = _registryOptions.GetAvailableLanguages();
            _syntaxModeCombo.SelectedItem = csharpLanguage;
            _syntaxModeCombo.SelectionChanged += SyntaxModeCombo_SelectionChanged;

            string scopeName = _registryOptions.GetScopeByLanguageId(csharpLanguage.Id);

            _textEditor.Document = new TextDocument(ResourceLoader.LoadSampleFile(scopeName));
            _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(csharpLanguage.Id));

            _statusTextBlock = this.Find<TextBlock>("StatusText")!;

            var mainWindowVm = new MainWindowViewModel(_textMateInstallation, _registryOptions);
            foreach (ThemeName themeName in Enum.GetValues<ThemeName>())
            {
                var themeViewModel = new ThemeViewModel(themeName);
                mainWindowVm.AllThemes.Add(themeViewModel);
                if (themeName == ThemeName.LightPlus) mainWindowVm.SelectedTheme = themeViewModel;
            }

            DataContext = mainWindowVm;
        }

        private void TextMateInstallationOnAppliedTheme(object? sender, TextMate.Installation e)
        {
            ApplyThemeColorsToEditor(e);
            ApplyThemeColorsToWindow(e);
        }

        private void ApplyThemeColorsToEditor(TextMate.Installation e)
        {
            ApplyBrushAction(e, "editor.background", brush => _textEditor.Background = brush);
            ApplyBrushAction(e, "editor.foreground", brush => _textEditor.TextArea.Foreground = brush);

            if (!ApplyBrushAction(e, "editor.selectionBackground",
                    brush => _textEditor.TextArea.SelectionBrush = brush))
                if (!ApplyBrushAction(e, "editor.selectionHighlightBackground",
                        brush => _textEditor.TextArea.SelectionBrush = brush))
                    if (Application.Current!.TryGetResource("TextAreaSelectionBrush", out object? resourceObject))
                        if (resourceObject is IBrush brush)
                            _textEditor.TextArea.SelectionBrush = brush;

            if (!ApplyBrushAction(e, "editor.lineHighlightBackground",
                    brush => { _textEditor.TextArea.TextView.CurrentLineBackground = brush; }))
                _textEditor.TextArea.TextView.SetDefaultHighlightLineColors();

            //Todo: looks like the margin doesn't have a active line highlight, would be a nice addition
            if (!ApplyBrushAction(e, "editorLineNumber.foreground",
                    brush => _textEditor.LineNumbersForeground = brush))
                _textEditor.LineNumbersForeground = _textEditor.TextArea.Foreground;
            _textEditor.TextArea.TextView.CurrentLineBorder = new Pen(Brushes.Transparent, 0);
        }

        private void ApplyThemeColorsToWindow(TextMate.Installation e)
        {
            var panel = this.Find<StackPanel>("StatusBar");
            if (panel == null) return;

            if (!ApplyBrushAction(e, "statusBar.background", brush => panel.Background = brush))
                panel.Background = Brushes.Purple;

            if (!ApplyBrushAction(e, "statusBar.foreground", brush => _statusTextBlock.Foreground = brush))
                _statusTextBlock.Foreground = Brushes.White;

            //Applying the Editor background to the whole window for demo sake.
            ApplyBrushAction(e, "editor.background", brush => Background = brush);
            ApplyBrushAction(e, "editor.foreground", brush => Foreground = brush);
        }

        private bool ApplyBrushAction(TextMate.Installation e, string colorKeyNameFromJson,
            Action<IBrush> applyColorAction)
        {
            ArgumentNullException.ThrowIfNull(applyColorAction);
            if (!e.TryGetThemeColor(colorKeyNameFromJson, out string? colorString))
                return false;

            if (!Color.TryParse(colorString, out Color color))
                return false;

            var colorBrush = new SolidColorBrush(color);
            applyColorAction(colorBrush);
            return true;
        }

        private void Caret_PositionChanged(object? sender, EventArgs e)
        {
            _statusTextBlock.Text = string.Format("Line {0} Column {1}",
                _textEditor.TextArea.Caret.Line,
                _textEditor.TextArea.Caret.Column);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            _textMateInstallation.Dispose();
        }

        private void SyntaxModeCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var language = (Language)_syntaxModeCombo.SelectedItem!;

            if (_foldingManager != null)
            {
                _foldingManager.Clear();
                FoldingManager.Uninstall(_foldingManager);
            }

            string scopeName = _registryOptions.GetScopeByLanguageId(language.Id);

            _textMateInstallation.SetGrammar(null);
            _textEditor.Document = new TextDocument(ResourceLoader.LoadSampleFile(scopeName));
            _textMateInstallation.SetGrammar(scopeName);

            if (language.Id == "xml")
            {
                _foldingManager = FoldingManager.Install(_textEditor.TextArea);

                var strategy = new XmlFoldingStrategy();
                strategy.UpdateFoldings(_foldingManager, _textEditor.Document);
            }
        }

        private void OnExit(object? sender, RoutedEventArgs e)
        {
            var lifetime = Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            ArgumentNullException.ThrowIfNull(lifetime);
            lifetime.Shutdown();
        }
    }
}