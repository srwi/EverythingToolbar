using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace EverythingToolbar.Helpers
{
    public static class HighlightedText
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached(
            "Source",
            typeof(string),
            typeof(HighlightedText),
            new PropertyMetadata(null, OnSourceChanged)
        );

        public static string? GetSource(TextBlock element)
        {
            return (string?)element.GetValue(SourceProperty);
        }

        public static void SetSource(TextBlock element, string? value)
        {
            element.SetValue(SourceProperty, value);
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBlock textBlock)
                return;

            var input = e.NewValue as string ?? "";

            // Nothing to highlight: assigning Text keeps the text block on its plain-text path,
            // which is about twice as cheap to lay out as an inline collection. Reading
            // textBlock.Inlines would itself create the machinery this branch avoids.
            if (!input.Contains('*'))
            {
                textBlock.Text = input;
                return;
            }

            var segments = input.Split('*');
            var inlines = textBlock.Inlines;

            if (TryUpdateRuns(inlines, segments))
                return;

            inlines.Clear();

            for (var i = 0; i < segments.Length; i++)
            {
                if (segments[i].Length == 0)
                    continue;

                var run = new Run(segments[i]);
                if (IsMatch(i))
                    run.FontWeight = FontWeights.Bold;

                inlines.Add(run);
            }
        }

        private static bool TryUpdateRuns(InlineCollection inlines, string[] segments)
        {
            var inline = inlines.FirstInline;

            for (var i = 0; i < segments.Length; i++)
            {
                if (segments[i].Length == 0)
                    continue;

                if (inline is not Run run)
                    return false;

                // Read the successor first: writing Run.Text edits the backing text container,
                // which invalidates any enumerator over the collection.
                var next = run.NextInline;

                if (run.Text != segments[i])
                    run.Text = segments[i];

                // Clearing rather than assigning Normal keeps an unhighlighted segment inheriting
                // its weight, exactly as a freshly constructed run would.
                if (IsMatch(i))
                    run.FontWeight = FontWeights.Bold;
                else
                    run.ClearValue(TextElement.FontWeightProperty);

                inline = next;
            }

            // Runs left over from a longer previous value mean the collection has to be rebuilt.
            return inline == null;
        }

        // Everything brackets each match in asterisks, so the odd-numbered segments are the matches.
        private static bool IsMatch(int segmentIndex) => segmentIndex % 2 > 0;
    }
}
