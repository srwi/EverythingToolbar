using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using NLog;
using AutomationCondition = System.Windows.Automation.Condition;

namespace EverythingToolbar.Services
{
    /// <summary>
    /// The bounds of the taskbar's icon cluster (Start button plus the task buttons) and the elements a
    /// window placed on the taskbar must not overlap, all in screen pixels.
    /// </summary>
    internal readonly record struct TaskbarLayout(Rect? IconCluster, IReadOnlyList<Rect> Obstacles)
    {
        public bool IsMeasurable => IconCluster.HasValue || Obstacles.Count > 0;
    }

    /// <summary>
    /// Measures the Windows 11 taskbar through UI Automation. The element names and the tree shape below
    /// are the parts that vary between Windows builds.
    /// </summary>
    internal sealed class TaskbarLayoutProbe
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger(nameof(TaskbarLayoutProbe));

        // Taskbar children wider than this much of the bar are containers spanning it, not icons.
        private const double MaxIconClusterChildWidthRatio = 0.6;

        private const string TaskbarFrameAutomationId = "TaskbarFrame";
        private const string SystemTrayIconAutomationId = "SystemTrayIcon";
        private const string WidgetsButtonAutomationId = "WidgetsButton";

        private IntPtr _cachedHandle;
        private AutomationElement? _cachedFrame;
        private AutomationElement[]? _cachedTrayIcons;

        /// <param name="refreshElements">
        /// False re-reads the positions of the elements found last time instead of searching for them
        /// again. Valid while a reflow is settling, where things move but none come or go.
        /// </param>
        public TaskbarLayout Measure(IntPtr taskbarHandle, bool refreshElements)
        {
            var obstacles = new List<Rect>();
            Rect? iconCluster = null;

            try
            {
                var taskbar = AutomationElement.FromHandle(taskbarHandle);

                foreach (var icon in GetTrayIcons(taskbarHandle, taskbar, refreshElements))
                {
                    var iconRect = icon.Current.BoundingRectangle;
                    if (iconRect.Width > 0)
                        obstacles.Add(iconRect);
                }

                var frame = GetFrame(taskbarHandle, taskbar);
                if (frame != null)
                {
                    double maxIconWidth = taskbar.Current.BoundingRectangle.Width * MaxIconClusterChildWidthRatio;

                    foreach (
                        AutomationElement child in frame.FindAll(TreeScope.Children, AutomationCondition.TrueCondition)
                    )
                    {
                        var rect = child.Current.BoundingRectangle;
                        if (rect.Width <= 0)
                            continue;

                        if (child.Current.AutomationId is SystemTrayIconAutomationId or WidgetsButtonAutomationId)
                            obstacles.Add(rect);
                        else if (rect.Width <= maxIconWidth)
                            iconCluster = iconCluster.HasValue ? Rect.Union(iconCluster.Value, rect) : rect;
                    }
                }
            }
            catch (Exception ex)
            {
                // Most likely a cached element that went away with its taskbar; resolve them again next time.
                _cachedFrame = null;
                _cachedTrayIcons = null;
                Logger.Warn(ex, "Could not measure the taskbar layout");
            }

            return new TaskbarLayout(iconCluster, obstacles);
        }

        /// <summary>
        /// Finding the frame is a full descendant walk, and it outlives every reflow, so it is resolved
        /// once per taskbar rather than on each measurement.
        /// </summary>
        private AutomationElement? GetFrame(IntPtr taskbarHandle, AutomationElement taskbar)
        {
            if (_cachedFrame == null || _cachedHandle != taskbarHandle)
            {
                _cachedFrame = taskbar.FindFirst(TreeScope.Descendants, ById(TaskbarFrameAutomationId));
                _cachedHandle = taskbarHandle;
            }

            return _cachedFrame;
        }

        /// <summary>
        /// Depending on the Windows build the tray icons sit outside the taskbar frame, so they are
        /// searched from the root rather than among the frame's children — the most expensive part of a
        /// measurement, and the tray does not move while task buttons reflow.
        /// </summary>
        private AutomationElement[] GetTrayIcons(IntPtr taskbarHandle, AutomationElement taskbar, bool refresh)
        {
            if (refresh || _cachedTrayIcons == null || _cachedHandle != taskbarHandle)
            {
                _cachedTrayIcons = taskbar
                    .FindAll(TreeScope.Descendants, ById(SystemTrayIconAutomationId))
                    .Cast<AutomationElement>()
                    .ToArray();
                _cachedHandle = taskbarHandle;
            }

            return _cachedTrayIcons;
        }

        private static PropertyCondition ById(string automationId) =>
            new(AutomationElement.AutomationIdProperty, automationId);
    }
}
