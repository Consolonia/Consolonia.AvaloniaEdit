using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Search;

namespace Consolonia.AvaloniaEdit
{
    /// <summary>
    ///     This attached property is used to enable console style search highlighting in AvaloniaEdit TextEditor.
    /// </summary>
    /// <remarks>
    ///     It is automatically added using the Consolonia.AvaloniaEdit Theme setter.
    ///     It can be explicitly added to a text editor XAML using: console:SearchHighlight.UseConsole="True"
    ///     It can be explicitly added to a text editor in code using: SearchHighlight.SetUseConsole(textEditor, true);
    ///     The theme also sets TextEditor.SearchResultsBrush to Transparent, set it to a color to get the stock
    ///     background marker back in addition to the underline.
    /// </remarks>
    public sealed class SearchHighlight
    {
        public static readonly AttachedProperty<bool> UseConsoleProperty =
            AvaloniaProperty.RegisterAttached<SearchHighlight, TextEditor, bool>("UseConsole");

        private static readonly AttachedProperty<Highlighter> HighlighterProperty =
            AvaloniaProperty.RegisterAttached<SearchHighlight, TextEditor, Highlighter>("Highlighter");

        static SearchHighlight()
        {
            UseConsoleProperty.Changed.AddClassHandler<TextEditor>((textEditor, e) =>
            {
                textEditor.GetValue(HighlighterProperty)?.Detach();
                textEditor.SetValue(HighlighterProperty,
                    (bool)e.NewValue ? new Highlighter(textEditor) : null);
            });
        }

        public static void SetUseConsole(TextEditor textEditor, bool value)
        {
            textEditor.SetValue(UseConsoleProperty, value);
        }

        public static bool GetUseConsole(TextEditor textEditor)
        {
            return textEditor.GetValue(UseConsoleProperty);
        }

        /// <summary>
        ///     Keeps a <see cref="SearchResultDecorationTransformer" /> in sync with the editor's search panel.
        /// </summary>
        private sealed class Highlighter
        {
            private readonly TextEditor _textEditor;
            private readonly SearchResultDecorationTransformer _transformer = new();
            private bool _detached;
            private SearchPanel _searchPanel;

            public Highlighter(TextEditor textEditor)
            {
                _textEditor = textEditor;
                textEditor.TextArea.TextView.LineTransformers.Add(_transformer);
                textEditor.TemplateApplied += OnTemplateApplied;
                // the template may already have been applied by the time we are switched on
                HookSearchPanel();
            }

            public void Detach()
            {
                _detached = true;
                _textEditor.TemplateApplied -= OnTemplateApplied;
                UnhookSearchPanel();
                _textEditor.TextArea.TextView.LineTransformers.Remove(_transformer);
                _textEditor.TextArea.TextView.Redraw();
            }

            private void OnTemplateApplied(object sender, TemplateAppliedEventArgs e)
            {
                // TextEditor creates its search panel in OnApplyTemplate, which runs before this event is raised.
                HookSearchPanel();
            }

            private void HookSearchPanel()
            {
                SearchPanel searchPanel = _textEditor.SearchPanel;
                if (ReferenceEquals(searchPanel, _searchPanel))
                    return;

                UnhookSearchPanel();

                _searchPanel = searchPanel;
                if (_searchPanel == null)
                    return;

                _searchPanel.SearchOptionsChanged += OnSearchOptionsChanged;
                _searchPanel.AttachedToVisualTree += OnSearchPanelAttachmentChanged;
                _searchPanel.DetachedFromVisualTree += OnSearchPanelAttachmentChanged;
                UpdateStrategy();
            }

            private void UnhookSearchPanel()
            {
                if (_searchPanel == null)
                    return;

                _searchPanel.SearchOptionsChanged -= OnSearchOptionsChanged;
                _searchPanel.AttachedToVisualTree -= OnSearchPanelAttachmentChanged;
                _searchPanel.DetachedFromVisualTree -= OnSearchPanelAttachmentChanged;
                _searchPanel = null;
            }

            private void OnSearchOptionsChanged(object sender, SearchOptionsChangedEventArgs e)
            {
                UpdateStrategy();
            }

            private void OnSearchPanelAttachmentChanged(object sender, VisualTreeAttachmentEventArgs e)
            {
                // Opening and closing the panel adds and removes it from the text area, but SearchPanel.Close()
                // only flips IsClosed after it has detached itself, so let the call finish before we look.
                Dispatcher.UIThread.Post(UpdateStrategy);
            }

            private void UpdateStrategy()
            {
                if (_detached)
                    return;

                ISearchStrategy strategy = CreateStrategy();
                if (strategy == null
                        ? _transformer.Strategy == null
                        : strategy.Equals(_transformer.Strategy))
                    return;

                _transformer.Strategy = strategy;
                // line transformers only run when the visual lines are built, so the results have to be rebuilt
                _textEditor.TextArea.TextView.Redraw();
            }

            private ISearchStrategy CreateStrategy()
            {
                if (_searchPanel is not { IsOpened: true } || string.IsNullOrEmpty(_searchPanel.SearchPattern))
                    return null;

                try
                {
                    return SearchStrategyFactory.Create(_searchPanel.SearchPattern,
                        !_searchPanel.MatchCase,
                        _searchPanel.WholeWords,
                        _searchPanel.UseRegex ? SearchMode.RegEx : SearchMode.Normal);
                }
                catch (SearchPatternException)
                {
                    // half typed regular expression, there is nothing to highlight yet
                    return null;
                }
            }
        }
    }
}
