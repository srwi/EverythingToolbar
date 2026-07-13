namespace EverythingToolbar.Core.Helpers
{

    public sealed record FocusSearchBoxRequest;

    public sealed record SearchBoxFocusedNotification;

    public sealed record ToolbarFocusChanged(bool IsFocused);

    public sealed record DeskbandUnfocusRequest;

    public sealed record SearchWindowHidingMessage;

    public sealed record SearchWindowActiveChanged(bool IsActive);
}