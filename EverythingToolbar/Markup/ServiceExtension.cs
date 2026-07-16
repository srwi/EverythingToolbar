using System;
using System.Windows.Markup;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace EverythingToolbar.Markup
{
    public sealed class ServiceExtension : MarkupExtension
    {
        public Type Type { get; set; } = null!;

        public ServiceExtension() { }

        public ServiceExtension(Type type) => Type = type;

        public override object ProvideValue(IServiceProvider serviceProvider) =>
            Ioc.Default.GetRequiredService(Type);
    }
}