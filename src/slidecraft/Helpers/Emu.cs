namespace slidecraft.Helpers;

/// <summary>
/// Constants for converting between WPF points (DIPs) and English Metric Units (EMU)
/// used by the Open XML SDK. The slide canvas is 960 × 540 points (16:9).
/// </summary>
public static class Emu
{
    /// <summary>EMU per point (1 pt = 12 700 EMU).</summary>
    public const long PerPoint = 12_700;

    /// <summary>Slide width in points.</summary>
    public const int SlideW = 960;

    /// <summary>Slide height in points.</summary>
    public const int SlideH = 540;

    /// <summary>Slide width in EMU.</summary>
    public const long SlideWidthEmu = SlideW * PerPoint;  // 12 192 000

    /// <summary>Slide height in EMU.</summary>
    public const long SlideHeightEmu = SlideH * PerPoint;  // 6 858 000

    /// <summary>Convert points to EMU.</summary>
    public static long ToEmu(double points) => (long)(points * PerPoint);

    /// <summary>Convert EMU to points.</summary>
    public static double ToPoints(long emu) => emu / (double)PerPoint;
}
