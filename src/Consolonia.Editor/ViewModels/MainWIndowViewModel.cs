using System.Collections.ObjectModel;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.TextMate;
using ReactiveUI;
using TextMateSharp.Grammars;

namespace Consolonia.Editor.ViewModels
{
    public class MainWindowViewModel(TextMate.Installation textMateInstallation, RegistryOptions registryOptions)
        : ReactiveObject
    {
        private ThemeViewModel? _selectedTheme;
        public ObservableCollection<ThemeViewModel> AllThemes { get; } = [];

        public ThemeViewModel SelectedTheme
        {
            get => _selectedTheme!;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedTheme, value);
                textMateInstallation.SetTheme(registryOptions.LoadTheme(value.ThemeName));
            }
        }

        public void CopyMouseCommmand(TextArea textArea)
        {
            ApplicationCommands.Copy.Execute(null, textArea);
        }

        public void CutMouseCommand(TextArea textArea)
        {
            ApplicationCommands.Cut.Execute(null, textArea);
        }

        public void PasteMouseCommmand(TextArea textArea)
        {
            ApplicationCommands.Paste.Execute(null, textArea);
        }

        public void SelectAllMouseCommmand(TextArea textArea)
        {
            ApplicationCommands.SelectAll.Execute(null, textArea);
        }

        // Undo Status is not given back to disable it's item in ContextFlyout; therefore it's not being used yet.
        public void UndoMouseCommmand(TextArea textArea)
        {
            ApplicationCommands.Undo.Execute(null, textArea);
        }
    }
}