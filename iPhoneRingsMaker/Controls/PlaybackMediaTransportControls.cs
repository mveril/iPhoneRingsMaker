using iPhoneRingsMaker.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace iPhoneRingsMaker.Controls;

public sealed class PlaybackMediaTransportControls : MediaTransportControls
{
    private CommandBar? _commandBar;
    private AppBarButton? _skipBackwardButton;
    private AppBarButton? _skipForwardButton;
    private int _skipIntervalSeconds = 5;

    public int SkipIntervalSeconds
    {
        get => _skipIntervalSeconds;
        set
        {
            _skipIntervalSeconds = Math.Clamp(value, 1, 10);
            UpdateButtonLabel(_skipBackwardButton, "Media_SkipBackward");
            UpdateButtonLabel(_skipForwardButton, "Media_SkipForward");
        }
    }

    public event EventHandler<int>? SkipRequested;

    public PlaybackMediaTransportControls()
    {
        DefaultStyleKey = typeof(MediaTransportControls);
    }

    protected override void OnApplyTemplate()
    {
        RemoveCustomButtons();
        base.OnApplyTemplate();

        _commandBar = GetTemplateChild("MediaControlsCommandBar") as CommandBar;
        if (_commandBar is null)
        {
            return;
        }

        _skipBackwardButton = CreateSkipButton(
            "\uEB9E",
            "Media_SkipBackward",
            "PlaybackSkipBackwardButton",
            -1);
        _skipForwardButton = CreateSkipButton(
            "\uEB9D",
            "Media_SkipForward",
            "PlaybackSkipForwardButton",
            1);

        var playPauseIndex = _commandBar.PrimaryCommands
            .OfType<FrameworkElement>()
            .Select((element, index) => (element, index))
            .FirstOrDefault(item => item.element.Name == "PlayPauseButton")
            .index;

        _commandBar.PrimaryCommands.Insert(playPauseIndex, _skipBackwardButton);
        _commandBar.PrimaryCommands.Insert(playPauseIndex + 2, _skipForwardButton);
    }

    private AppBarButton CreateSkipButton(
        string glyph,
        string resourceKey,
        string automationId,
        int direction)
    {
        var button = new AppBarButton
        {
            Icon = new FontIcon { Glyph = glyph },
        };
        AutomationProperties.SetAutomationId(button, automationId);
        MediaTransportControlsHelper.SetDropoutOrder(button, 5);
        UpdateButtonLabel(button, resourceKey);
        button.Click += (_, _) => SkipRequested?.Invoke(this, direction * SkipIntervalSeconds);
        return button;
    }

    private void UpdateButtonLabel(AppBarButton? button, string resourceKey)
    {
        if (button is null)
        {
            return;
        }

        var label = string.Format(
            ResourceExtensions.GetLocalized(resourceKey),
            SkipIntervalSeconds);
        button.Label = label;
        AutomationProperties.SetName(button, label);
        ToolTipService.SetToolTip(button, label);
    }

    private void RemoveCustomButtons()
    {
        if (_commandBar is null)
        {
            return;
        }

        if (_skipBackwardButton is not null)
        {
            _commandBar.PrimaryCommands.Remove(_skipBackwardButton);
        }

        if (_skipForwardButton is not null)
        {
            _commandBar.PrimaryCommands.Remove(_skipForwardButton);
        }
    }
}
