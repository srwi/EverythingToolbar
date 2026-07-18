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

            textBlock.Inlines.Clear();

            if (e.NewValue is not string input || input.Length == 0)
                return;

            string[] segments = input.Split('*');
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i].Length == 0)
                    continue;

                var run = new Run(segments[i]);
                if (i % 2 > 0)
                    run.FontWeight = FontWeights.Bold;

                textBlock.Inlines.Add(run);
            }
        }
    }
}
