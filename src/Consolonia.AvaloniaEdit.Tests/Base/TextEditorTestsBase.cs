using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Consolonia.Core.Drawing.PixelBufferImplementation;
using Consolonia.NUnit;
using NUnit.Framework;

namespace Consolonia.AvaloniaEdit.Tests.Base
{
    /// <summary>
    ///     Base class for tests driving the text editor of <see cref="TestApp" />.
    /// </summary>
    /// <remarks>
    ///     The Consolonia test app is a static singleton for the whole test run, so the editor is put back into a
    ///     known state before every test instead of relying on a fresh app.
    /// </remarks>
    public abstract class TextEditorTestsBase : ConsoloniaAppTestBase<TestApp>
    {
        protected TextEditorTestsBase() : base(new PixelBufferSize(BufferWidth, BufferHeight))
        {
            Args = [];
        }

        protected const ushort BufferWidth = 120;
        protected const ushort BufferHeight = 22;

        protected static TextEditor Editor { get; private set; }

        [OneTimeSetUp]
        public async Task FindEditor()
        {
            await UITest.WaitRendered().ConfigureAwait(true);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var lifetime = (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;
                Editor = lifetime.MainWindow!.GetVisualDescendants().OfType<TextEditor>().Single();
            });
        }

        [SetUp]
        public async Task ResetEditor()
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Editor.SearchPanel is { IsOpened: true } searchPanel)
                    searchPanel.Close();

                Editor.Document.Text = TestApp.SampleText;
                Editor.TextArea.ClearSelection();
                Editor.CaretOffset = 0;
                ((IScrollable)Editor.TextArea.TextView).Offset = default;
                Editor.TextArea.Focus();
            });
            await WaitRenderedAsync().ConfigureAwait(true);
        }

        /// <summary>
        ///     Waits for the rendering to settle. Revealing a search result schedules work on the dispatcher, so a
        ///     single render pass is not enough to observe the final state.
        /// </summary>
        protected static async Task WaitRenderedAsync()
        {
            await UITest.WaitRendered().ConfigureAwait(true);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).GetTask()
                .ConfigureAwait(true);
            await UITest.WaitRendered().ConfigureAwait(true);
        }

        /// <summary>
        ///     Opens the search panel and types the pattern into it, one character at a time as a user would.
        /// </summary>
        protected static async Task SearchAsync(string pattern)
        {
            await UITest.KeyInput(Avalonia.Input.Key.F, Avalonia.Input.RawInputModifiers.Control)
                .ConfigureAwait(true);
            await WaitRenderedAsync().ConfigureAwait(true);
            await UITest.StringInput(pattern).ConfigureAwait(true);
            await WaitRenderedAsync().ConfigureAwait(true);
        }

        /// <summary>
        ///     The console cell a document offset is drawn in, or <c>null</c> when it is scrolled out of view.
        /// </summary>
        protected static Task<PixelPoint?> CellOfAsync(int documentOffset)
        {
            return Dispatcher.UIThread.InvokeAsync(() => CellOf(documentOffset)).GetTask();
        }

        /// <summary>
        ///     Every cell a match of <paramref name="pattern" /> occupies on screen, found by walking the document
        ///     rather than the rendered buffer so that the two can be compared.
        /// </summary>
        protected static Task<HashSet<PixelPoint>> CellsOfOccurrencesAsync(string pattern)
        {
            return Dispatcher.UIThread.InvokeAsync(() =>
            {
                var cells = new HashSet<PixelPoint>();
                string text = Editor.Document.Text;
                for (int offset = text.IndexOf(pattern, StringComparison.Ordinal);
                     offset >= 0;
                     offset = text.IndexOf(pattern, offset + 1, StringComparison.Ordinal))
                    for (int i = 0; i < pattern.Length; i++)
                    {
                        PixelPoint? cell = CellOf(offset + i);
                        if (cell != null)
                            cells.Add(cell.Value);
                    }

                return cells;
            }).GetTask();
        }

        /// <summary>
        ///     Must be called on the UI thread, the editor and its text view belong to it.
        /// </summary>
        private static PixelPoint? CellOf(int documentOffset)
        {
            TextView textView = Editor.TextArea.TextView;
            DocumentLine documentLine = Editor.Document.GetLineByOffset(documentOffset);
            VisualLine visualLine = textView.GetVisualLine(documentLine.LineNumber);
            if (visualLine == null)
                return null;

            var window = ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!)
                .MainWindow!;
            Point origin = textView.TranslatePoint(new Point(0, 0), window) ?? new Point(0, 0);

            var cell = new PixelPoint(
                (int)(origin.X + documentOffset - documentLine.Offset - textView.ScrollOffset.X),
                (int)(origin.Y + visualLine.VisualTop - textView.ScrollOffset.Y));

            return cell.X < 0 || cell.Y < 0 || cell.X >= BufferWidth || cell.Y >= BufferHeight ? null : cell;
        }

        protected static Pixel CellAt(PixelPoint cell)
        {
            return UITest.PixelBuffer[(ushort)cell.X, (ushort)cell.Y];
        }

        protected static string TextAt(PixelPoint cell, int length)
        {
            var text = new StringBuilder();
            for (int i = 0; i < length; i++)
                text.Append(CellAt(cell.WithX(cell.X + i)).Foreground.Symbol.GetText());
            return text.ToString();
        }

        /// <summary>
        ///     The widest the sample document gets. The search panel is anchored to the right and paints selected
        ///     text of its own, so scans of the document area stop before it.
        /// </summary>
        protected const int DocumentColumns = 40;

        /// <summary>
        ///     The glyphs of every cell in the document area painted with the given background, read in reading
        ///     order. This deliberately goes through the rendered buffer rather than through the text view's own
        ///     coordinates, so that it can catch a rectangle drawn a row away from the glyphs it belongs to.
        /// </summary>
        protected static string GlyphsOnBackground(Color background)
        {
            PixelBuffer buffer = UITest.PixelBuffer;
            var glyphs = new StringBuilder();
            for (ushort y = 0; y < buffer.Height; y++)
            for (ushort x = 0; x < DocumentColumns; x++)
                if (buffer[x, y].Background.Color == background)
                    glyphs.Append(buffer[x, y].Foreground.Symbol.GetText());

            return glyphs.ToString();
        }

        /// <summary>
        ///     Every cell currently carrying the given text decoration.
        /// </summary>
        protected static HashSet<PixelPoint> CellsDecoratedWith(TextDecorationLocation decoration)
        {
            var cells = new HashSet<PixelPoint>();
            PixelBuffer buffer = UITest.PixelBuffer;
            for (ushort y = 0; y < buffer.Height; y++)
            for (ushort x = 0; x < buffer.Width; x++)
                if (buffer[x, y].Foreground.TextDecoration == decoration)
                    cells.Add(new PixelPoint(x, y));

            return cells;
        }

        /// <summary>
        ///     A picture of the buffer, so that a failure shows what was on screen instead of only a cell count.
        /// </summary>
        protected static string DescribeBuffer()
        {
            PixelBuffer buffer = UITest.PixelBuffer;
            var description = new StringBuilder();
            for (ushort y = 0; y < buffer.Height; y++)
            {
                var text = new StringBuilder();
                var decorations = new StringBuilder();
                for (ushort x = 0; x < buffer.Width; x++)
                {
                    string symbol = buffer[x, y].Foreground.Symbol.GetText();
                    text.Append(string.IsNullOrEmpty(symbol) ? " " : symbol);
                    decorations.Append(buffer[x, y].Foreground.TextDecoration switch
                    {
                        TextDecorationLocation.Underline => '_',
                        TextDecorationLocation.Strikethrough => '-',
                        _ => ' '
                    });
                }

                description.AppendLine($"{y,3}|{text.ToString().TrimEnd()}");
                if (decorations.ToString().Trim().Length > 0)
                    description.AppendLine($"   |{decorations.ToString().TrimEnd()}");
            }

            return description.ToString();
        }
    }
}
