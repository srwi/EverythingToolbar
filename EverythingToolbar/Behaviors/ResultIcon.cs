using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using EverythingToolbar.Core.Data;

namespace EverythingToolbar.Behaviors
{
    public static class ResultIcon
    {
        public static readonly DependencyProperty ResultProperty = DependencyProperty.RegisterAttached(
            "Result",
            typeof(SearchResult),
            typeof(ResultIcon),
            new PropertyMetadata(null, OnResultChanged)
        );

        public static void SetResult(DependencyObject element, SearchResult? value) =>
            element.SetValue(ResultProperty, value);

        public static SearchResult? GetResult(DependencyObject element) =>
            (SearchResult?)element.GetValue(ResultProperty);

        public static readonly DependencyProperty PreviewResultProperty = DependencyProperty.RegisterAttached(
            "PreviewResult",
            typeof(SearchResult),
            typeof(ResultIcon),
            new PropertyMetadata(null, OnPreviewResultChanged)
        );

        public static void SetPreviewResult(DependencyObject element, SearchResult? value) =>
            element.SetValue(PreviewResultProperty, value);

        public static SearchResult? GetPreviewResult(DependencyObject element) =>
            (SearchResult?)element.GetValue(PreviewResultProperty);

        private static void OnResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Image image)
                return;

            if (e.NewValue is not SearchResult result)
            {
                BindingOperations.ClearBinding(image, Image.SourceProperty);
                return;
            }

            var images = ResultImageCache.Get(result);
            images.EnsureIconLoading();
            BindingOperations.SetBinding(
                image,
                Image.SourceProperty,
                new Binding(nameof(ResultImages.Icon)) { Source = images }
            );
        }

        private static void OnPreviewResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Image image)
                return;

            if (e.NewValue is not SearchResult result)
            {
                BindingOperations.ClearBinding(image, Image.SourceProperty);
                return;
            }

            var images = ResultImageCache.Get(result);
            images.EnsurePreviewLoading();
            BindingOperations.SetBinding(
                image,
                Image.SourceProperty,
                new Binding(nameof(ResultImages.PreviewImage)) { Source = images }
            );
        }
    }
}
