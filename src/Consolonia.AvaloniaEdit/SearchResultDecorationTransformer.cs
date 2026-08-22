using System;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Search;

namespace Consolonia.AvaloniaEdit
{
    /// <summary>
    ///     Underlines the search results of a <see cref="SearchPanel" />.
    /// </summary>
    /// <remarks>
    ///     AvaloniaEdit marks search results with a background renderer which paints a rectangle behind them. We mark
    ///     them by decorating the glyphs instead, because a decoration is applied to the console cells the glyphs are
    ///     in, and so it can not be off by a row. Note that the same can not be done from a background renderer:
    ///     background renderers are drawn before the text layer, and a decoration applied to a cell which does not hold
    ///     a glyph yet is dropped when the glyph is later blended into it.
    /// </remarks>
    internal sealed class SearchResultDecorationTransformer : DocumentColorizingTransformer
    {
        private TextDecorationCollection _underline;
        private double _underlineThickness;

        /// <summary>
        ///     The strategy locating the results to underline, or <c>null</c> when nothing is to be underlined.
        /// </summary>
        public ISearchStrategy Strategy { get; set; }

        protected override void ColorizeLine(DocumentLine line)
        {
            ISearchStrategy strategy = Strategy;
            if (strategy == null || line.Length == 0)
                return;

            foreach (ISearchResult result in strategy.FindAll(CurrentContext.Document, line.Offset, line.Length))
            {
                int startOffset = Math.Max(result.Offset, line.Offset);
                int endOffset = Math.Min(result.EndOffset, line.EndOffset);
                if (endOffset <= startOffset)
                    continue;

                ChangeLinePart(startOffset, endOffset, ApplyUnderline);
            }
        }

        private void ApplyUnderline(VisualLineElement element)
        {
            // Consolonia turns a line drawn with the console typeface's underline thickness into the underline
            // decoration of the cells it covers, so the thickness has to be taken from the typeface verbatim.
            // This is the same trick DecorationsFontMetricsTransformer applies to decorations coming from elsewhere,
            // we do it ourselves so that we do not depend on the order of the line transformers.
            double thickness = element.TextRunProperties.Typeface.GlyphTypeface.Metrics.UnderlineThickness;
            if (_underline == null || !_underlineThickness.Equals(thickness))
            {
                _underlineThickness = thickness;
                _underline = new TextDecorationCollection
                {
                    new TextDecoration
                    {
                        Location = TextDecorationLocation.Underline,
                        StrokeThicknessUnit = TextDecorationUnit.Pixel,
                        StrokeThickness = thickness
                    }
                };
            }

            element.TextRunProperties.SetTextDecorations(_underline);
        }
    }
}