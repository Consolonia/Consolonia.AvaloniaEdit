using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using AvaloniaEdit.Rendering;

namespace Consolonia.AvaloniaEdit
{
    /// <summary>
    ///     Keeps <see cref="TextView.ScrollOffset" /> on whole console cells.
    /// </summary>
    /// <remarks>
    ///     AvaloniaEdit scrolls in device pixels and happily lands on a fraction of a line. TextView.MakeVisible()
    ///     for example reveals a rectangle it considers taller than the viewport by scrolling to the middle of it,
    ///     which is half a line for a single line rectangle, and that is what happens when the search panel reveals
    ///     a result. Half a line is a rounding detail on a pixel backend, but a console cell can not be split, and
    ///     the two ways a line is painted round it differently: the glyphs of a line sitting at y = -0.5 are drawn
    ///     on the row above and clipped away, while the rectangles BackgroundGeometryBuilder produces for that same
    ///     line are snapped to whole pixels and land on row 0. Selections, the current line highlight and search
    ///     markers then show up one row below the text they belong to, until any ordinary scrolling puts the offset
    ///     back on a whole line.
    ///     Rounding down rather than to the nearest cell keeps the line the scroll was meant to reveal fully visible.
    /// </remarks>
    internal static class ConsoleScrollOffset
    {
        public static void Attach(TextView textView)
        {
            // subscribing is idempotent so that switching the attached property on twice does not snap twice
            textView.ScrollOffsetChanged -= OnScrollOffsetChanged;
            textView.ScrollOffsetChanged += OnScrollOffsetChanged;
            Snap(textView);
        }

        public static void Detach(TextView textView)
        {
            textView.ScrollOffsetChanged -= OnScrollOffsetChanged;
        }

        private static void OnScrollOffsetChanged(object sender, EventArgs e)
        {
            // Snap assigns the offset again, which raises this a second time with an already aligned offset
            Snap((TextView)sender);
        }

        private static void Snap(TextView textView)
        {
            Vector offset = textView.ScrollOffset;
            var aligned = new Vector(Math.Floor(offset.X), Math.Floor(offset.Y));
            if (aligned.NearlyEquals(offset))
                return;

            ((IScrollable)textView).Offset = aligned;
        }
    }
}
