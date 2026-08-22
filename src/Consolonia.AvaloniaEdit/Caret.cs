using System;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using Consolonia.Controls;
using Consolonia.Controls.Brushes;

namespace Consolonia.AvaloniaEdit
{
    /// <summary>
    ///     This attached property is used to enable console style caret in AvaloniaEdit TextEditor.
    /// </summary>
    /// <remarks>
    ///     It is autoamtically added using the Consolonia.AvaloniaEdit Theme setter
    ///     It can be explitily added to a text editor XAML using: console:Caret.UseConsole="True"
    ///     It can be explicitly added to a text editor in code using: Caret.SetUseConsole(textEditor, true);
    /// </remarks>
    public sealed class Caret
    {
        public static readonly AttachedProperty<bool> UseConsoleProperty =
            AvaloniaProperty.RegisterAttached<Caret, TextEditor, bool>("UseConsole");

        static Caret()
        {
            UseConsoleProperty.Changed.AddClassHandler<TextEditor>((textEditor, e) =>
            {
                textEditor.Options.LineHeightFactor = 1.0; // we always want to set the default line height factor.

                bool value = (bool)e.NewValue;
                if (value)
                {
                    textEditor.TextArea.TextView.LineTransformers.Add(new DecorationsFontMetricsTransformer());

                    // AvaloniaEdit is free to scroll to half a line, which a console can not show. Snapping the
                    // offset to whole cells keeps the glyphs of a line and the rectangles drawn behind it (the
                    // selection, the current line highlight, search markers) on the same row.
                    ConsoleScrollOffset.Attach(textEditor.TextArea.TextView);

                    textEditor.TextArea.Caret.CaretBrush = new MoveConsoleCaretToPositionBrush
                        { CaretStyle = CaretStyle.BlinkingBar };

                    {
                        // This is needed because we can not render more than one caret at once, which happens during search
                        Visual caretLayer = textEditor.TextArea.TextView.GetVisualDescendants()
                            .Single(visual => visual.GetType().FullName == "AvaloniaEdit.Editing.CaretLayer");

                        //todo: try to move this binding to style
                        caretLayer.Bind(Visual.IsVisibleProperty, new Binding
                        {
                            RelativeSource = new RelativeSource
                                { Mode = RelativeSourceMode.FindAncestor, AncestorType = typeof(TextArea) },
                            Path = nameof(Control.IsFocused)
                        });

                        // We don't need the internal blinking animation because the hardware caret already blinks.
                        // Unhook the Tick handler from the internal blink timer so _blink stays true forever,
                        // leaving the actual blinking to the terminal's own hardware caret.
                        Type caretLayerType = caretLayer.GetType();
                        FieldInfo timerField = caretLayerType.GetField("_caretBlinkTimer",
                            BindingFlags.NonPublic | BindingFlags.Instance)!;
                        MethodInfo tickMethod = caretLayerType.GetMethod("CaretBlinkTimer_Tick",
                            BindingFlags.NonPublic | BindingFlags.Instance)!;
                        var caretBlinkTimer = (DispatcherTimer)timerField.GetValue(caretLayer);
                        var tickHandler =
                            (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), caretLayer, tickMethod);
                        caretBlinkTimer.Tick -= tickHandler;
                        caretBlinkTimer.Stop();
                    }

                    textEditor.TextArea.PropertyChanged += TextArea_PropertyChanged;

                    // The built in LineNumberMargin miscalculates the top of the line, 
                    // we substitute ours with one which works correctly.
                    for (int i = 0; i < textEditor.TextArea.LeftMargins.Count; i++)
                        if (textEditor.TextArea.LeftMargins[i] is LineNumberMargin)
                        {
                            textEditor.TextArea.LeftMargins[i] = new ConsoleLineNumberMargin();
                            break;
                        }
                }
                else
                {
                    /*todo: brush setup and restoration: textEditor.TextArea.Caret.CaretBrush = oldBrush;*/
                    ConsoleScrollOffset.Detach(textEditor.TextArea.TextView);
                    textEditor.TextArea.PropertyChanged -= TextArea_PropertyChanged;
                }
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

        private static void TextArea_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            // monitor OverstrikeMode property changes to update caret style to match
            if (e.Property == TextArea.OverstrikeModeProperty)
            {
                var textArea = (TextArea)sender;
                if (textArea.Caret.CaretBrush is MoveConsoleCaretToPositionBrush caretBrush)
                    caretBrush.CaretStyle = (bool)e.NewValue
                        ? CaretStyle.BlinkingBlock
                        : CaretStyle.BlinkingBar;
            }
        }
    }
}