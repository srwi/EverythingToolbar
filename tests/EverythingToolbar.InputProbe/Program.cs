using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using Config.Net;
using EverythingToolbar;
using EverythingToolbar.App;
using EverythingToolbar.App.Search;
using EverythingToolbar.Behaviors;
using EverythingToolbar.Controls;
using EverythingToolbar.Core.Search;
using EverythingToolbar.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xaml.Behaviors;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (!args.Contains("--send-input"))
        {
            Console.WriteLine("Opt-in desktop integration probe. Keep the keyboard/mouse idle while it runs.");
            Console.WriteLine("Use --send-input [--fake] [--manual] [--unhosted]. See tests/README.md.");
            return 0;
        }

        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        var settings = SettingsProxy.Create(
            new ConfigurationBuilder<IToolbarSettings>().UseInMemoryDictionary().Build()
        );
        settings.IsAnimationsDisabled = true;
        settings.IsSearchAsYouType = !args.Contains("--manual");
        var services = new ServiceCollection();
        services.AddSingleton(settings);
        // Reuse production registrations, but never AddSettings (disk persistence), SearchHost.Attach
        // (global hotkeys), TaskbarWindow (Explorer attachment), or the installed launcher process.
        var registrations = typeof(AppServices).Assembly.GetType("EverythingToolbar.ServiceCollectionExtensions")!;
        foreach (var method in new[] { "AddPlatformAdapters", "AddSearchEngine", "AddShellServices", "AddViewModels" })
            registrations.GetMethod(method)!.Invoke(null, new object[] { services });
        if (args.Contains("--fake"))
            services.AddSingleton<IEverythingClient, FakeClient>();
        using var provider = services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(provider);
        var client = provider.GetRequiredService<IEverythingClient>();
        client.SetInstanceName("");
        Console.WriteLine(
            $"Backend={(args.Contains("--fake") ? "deterministic fixture" : "Everything")} version={client.GetEverythingVersion()} live={settings.IsSearchAsYouType}"
        );
        var controller = provider.GetRequiredService<SearchWindowController>();
        var popup = provider.GetRequiredService<SearchWindow>();
        controller.SetIconMode(false);
        var toolbar = new ToolbarControl();
        var host = new Window
        {
            Content = toolbar,
            Title = "EverythingToolbar isolated input probe",
            Width = 400,
            Height = 100,
            Left = 100,
            Top = 100,
            ShowInTaskbar = false,
        };
        var placement = new SearchWindowPlacement(
            provider.GetRequiredService<TaskbarInfoProvider>(),
            settings,
            provider.GetRequiredService<WindowsPolicy>()
        )
        {
            PlacementTarget = toolbar,
        };
        Interaction.GetBehaviors(popup).Add(placement);
        host.Show();
        using var nativeHost = args.Contains("--unhosted") ? null : new NativeHost();
        var handle = new WindowInteropHelper(host).Handle;
        nativeHost?.Attach(handle);
        var box = (SearchBox)toolbar.FindName("SearchBox");
        var input = (TextBox)box.FindName("TextBox");
        var state = provider.GetRequiredService<SearchState>();
        var session = provider.GetRequiredService<SearchSession>();
        var keydowns = 0;
        var textInputs = 0;
        var resets = 0;
        var focusChanges = 0;
        input.PreviewKeyDown += (_, _) => keydowns++;
        input.PreviewTextInput += (_, e) => textInputs += e.Text.Length;
        input.LostKeyboardFocus += (_, _) => focusChanges++;
        session.ResultsReset += () => resets++;
        box.Focus();
        Pump(500);
        var failures = 0;
        try
        {
            foreach (var delay in new[] { 100, 75, 50 })
            {
                state.SearchTerm = "";
                Pump(100);
                if (!input.IsKeyboardFocusWithin)
                {
                    Console.WriteLine("ABORT: editor lost focus. No further input will be sent.");
                    failures++;
                    break;
                }
                input.SelectAll();
                keydowns = textInputs = resets = focusChanges = 0;
                const string expected = "everythingtoolbar";
                var sent = 0;
                var ui = Dispatcher.CurrentDispatcher;
                var done = new DispatcherFrame();
                Task.Run(() =>
                {
                    try
                    {
                        foreach (var c in expected)
                        {
                            if (!PhysicalKeys.Send(c, handle, nativeHost?.Handle ?? IntPtr.Zero))
                                break;
                            sent++;
                            Thread.Sleep(delay);
                        }
                        Thread.Sleep(1000); // Test-only drainage; no delay is added to the application.
                    }
                    finally
                    {
                        ui.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => done.Continue = false));
                    }
                });
                Dispatcher.PushFrame(done);
                var passed =
                    sent == expected.Length
                    && input.Text == expected
                    && focusChanges == 0
                    && (!settings.IsSearchAsYouType || state.SearchTerm == expected);
                Console.WriteLine(
                    $"{(passed ? "PASS" : "FAIL")} interval={delay}ms sent={sent} KeyDown={keydowns} TextInput={textInputs} resets={resets} focusChanges={focusChanges} text='{input.Text}'"
                );
                if (!passed)
                    failures++;
                if (sent != expected.Length || focusChanges != 0)
                    break; // User interference is an invalid trial, never a reason to steal focus again.
            }
        }
        finally
        {
            Interaction.GetBehaviors(popup).Remove(placement);
            app.Shutdown();
        }
        return failures == 0 ? 0 : 1;
    }

    private static void Pump(int milliseconds)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
        {
            Interval = TimeSpan.FromMilliseconds(milliseconds),
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            frame.Continue = false;
        };
        timer.Start();
        Dispatcher.PushFrame(frame);
    }
}
