using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.Styling;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using Consolonia.Themes;

namespace Consolonia.AvaloniaEdit.Tests
{
    /// <summary>
    ///     A bare text editor styled by the theme under test.
    /// </summary>
    /// <remarks>
    ///     The lines are kept short on purpose. The search panel is anchored to the top right of the text area, so
    ///     with a wide enough console it can not cover any of the text and the tests can assert on every cell a
    ///     document line occupies.
    /// </remarks>
    public class TestApp : Application
    {
        public const string SampleText = """
                                         class Sample
                                         {
                                             List<int> first;
                                             List<int> second;
                                             int count;

                                             void Add(int x)
                                             {
                                                 first.Add(x);
                                                 second.Add(x);
                                                 count++;
                                             }
                                         }
                                         """;

        public override void Initialize()
        {
            Styles.Add(new ModernTheme());
            Styles.Add(new StyleInclude(new Uri("avares://Consolonia.AvaloniaEdit.Tests/"))
            {
                Source = new Uri("avares://Consolonia.AvaloniaEdit/Theme.axaml")
            });
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                lifetime.MainWindow = new Window
                {
                    Content = new TextEditor
                    {
                        ShowLineNumbers = false,
                        Document = new TextDocument(SampleText)
                    }
                };

            base.OnFrameworkInitializationCompleted();
        }
    }
}
