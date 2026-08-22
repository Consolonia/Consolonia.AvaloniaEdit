using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using Consolonia.AvaloniaEdit.Tests.Base;
using Consolonia.NUnit;
using NUnit.Framework;

namespace Consolonia.AvaloniaEdit.Tests
{
    /// <summary>
    ///     Search results are marked by underlining them rather than by painting a rectangle behind them, see
    ///     <see cref="SearchHighlight" />.
    /// </summary>
    [TestFixture]
    public class SearchHighlightTests : TextEditorTestsBase
    {
        private const string Pattern = "List";

        [Test]
        public async Task SearchResultsAreUnderlined()
        {
            await SearchAsync(Pattern);

            HashSet<PixelPoint> expected = await CellsOfOccurrencesAsync(Pattern).ConfigureAwait(true);
            Assert.That(expected, Is.Not.Empty, "the sample text must contain visible matches");

            Assert.That(CellsDecoratedWith(TextDecorationLocation.Underline), Is.EquivalentTo(expected),
                $"exactly the cells of the matches must be underlined\n{DescribeBuffer()}");
        }

        [Test]
        public async Task UnderlinesAreRemovedWhenTheSearchPanelCloses()
        {
            await SearchAsync(Pattern);
            Assert.That(CellsDecoratedWith(TextDecorationLocation.Underline), Is.Not.Empty);

            await UITest.KeyInput(Key.Escape).ConfigureAwait(true);
            await WaitRenderedAsync().ConfigureAwait(true);

            Assert.That(CellsDecoratedWith(TextDecorationLocation.Underline), Is.Empty,
                $"closing the search panel must clear the underlines\n{DescribeBuffer()}");
        }

        /// <summary>
        ///     The active result is the selected one. Revealing it must not leave the text view scrolled to half a
        ///     line, otherwise its selection rectangle is drawn a row away from the text it selects.
        /// </summary>
        [Test]
        public async Task ActiveSearchResultIsOnTheSameCellsAsItsText()
        {
            await SearchAsync(Pattern);

            var active = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ISegment segment = Editor.TextArea.Selection.SurroundingSegment;
                var selectionBrush = (ISolidColorBrush)Editor.TextArea.SelectionBrush;
                return (Length: segment?.Length ?? 0, selectionBrush.Color);
            }).GetTask().ConfigureAwait(true);

            Assert.That(active.Length, Is.EqualTo(Pattern.Length), "the first result must be selected");

            Assert.That(GlyphsOnBackground(active.Color), Is.EqualTo(Pattern),
                $"the selection is not drawn on the cells holding the selected text\n{DescribeBuffer()}");
        }

        /// <summary>
        ///     A console cell can not be split, so a fractional scroll offset makes the glyphs of a line and the
        ///     rectangles drawn behind it round to different rows, see <see cref="ConsoleScrollOffset" />.
        /// </summary>
        [Test]
        public async Task RevealingAResultKeepsTheScrollOffsetOnWholeCells()
        {
            await SearchAsync(Pattern);

            Vector offset = await Dispatcher.UIThread.InvokeAsync(() => Editor.TextArea.TextView.ScrollOffset)
                .GetTask().ConfigureAwait(true);

            Assert.Multiple(() =>
            {
                Assert.That(offset.X % 1, Is.Zero, "horizontal scroll offset must be a whole number of cells");
                Assert.That(offset.Y % 1, Is.Zero, "vertical scroll offset must be a whole number of cells");
            });
        }
    }
}
