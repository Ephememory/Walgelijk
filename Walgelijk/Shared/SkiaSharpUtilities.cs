using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Walgelijk;

/// <summary>
/// Convienence functions for working with <see cref="SkiaSharp"/> images and data.
/// </summary>
public static class SkiaSharpUtilities
{
    /// <summary>
    /// Convert an <see cref="SKColor"/> to a <see cref="Walgelijk.Color"/>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color ToWalgelijk(this SKColor other) => new Color(other.Red, other.Green, other.Blue, other.Alpha);
}