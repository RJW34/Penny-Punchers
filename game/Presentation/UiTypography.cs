using Godot;
namespace StrikeLedger.Presentation;

/// <summary>Unmodified Barlow fonts, bundled with the SIL OFL in Fonts/OFL.txt.</summary>
public static class UiTypography
{
    public static Font Body=>GD.Load<FontFile>("res://Presentation/Fonts/Barlow-Medium.ttf");
    public static Font Heading=>GD.Load<FontFile>("res://Presentation/Fonts/BarlowCondensed-SemiBold.ttf");
}
