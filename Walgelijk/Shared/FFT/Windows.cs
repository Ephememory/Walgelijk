//This file contains modified code from the FftSharp library authored by Scott W Harden under the MIT license.
//https://github.com/swharden/FftSharp
//https://en.wikipedia.org/wiki/MIT_License

//Copyright(c) 2020 Scott W Harden

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Linq;
using System.Reflection;

namespace Walgelijk.FFT.Windows;

public interface IWindow
{
    /// <summary>
    /// Generate this window as a new array with the given length.
    /// Normalizing will scale the window so the sum of all points is 1.
    /// </summary>
    float[] Create(int size, bool normalize = false);

    /// <summary>
    /// Return a new array where this window was multiplied by the given signal.
    /// Normalizing will scale the window so the sum of all points is 1 prior to multiplication.
    /// </summary>
    float[] Apply(float[] input, bool normalize = false);

    /// <summary>
    /// Modify the given signal by multiplying it by this window IN PLACE.
    /// Normalizing will scale the window so the sum of all points is 1 prior to multiplication.
    /// </summary>
    void ApplyInPlace(float[] input, bool normalize = false);

    /// <summary>
    /// Single word name for this window
    /// </summary>
    string Name { get; }

    /// <summary>
    /// A brief description of what makes this window unique and what it is typically used for.
    /// </summary>
    string Description { get; }
}

public abstract class Window : IWindow
{
    public abstract string Name { get; }

    public abstract string Description { get; }

    public override string ToString() => Name;

    public abstract float[] Create(int size, bool normalize = false);

    /// <summary>
    /// Multiply the array by this window and return the result as a new array
    /// </summary>
    public float[] Apply(float[] input, bool normalize = false)
    {
        // TODO: save this window so it can be re-used if the next request is the same size
        float[] window = Create(input.Length, normalize);
        float[] output = new float[input.Length];
        for (int i = 0; i < input.Length; i++)
            output[i] = input[i] * window[i];
        return output;
    }

    /// <summary>
    /// Multiply the array by this window, modifying it in place
    /// </summary>
    public void ApplyInPlace(float[] input, bool normalize = false)
    {
        float[] window = Create(input.Length, normalize);
        for (int i = 0; i < input.Length; i++)
            input[i] = input[i] * window[i];
    }

    internal static void NormalizeInPlace(float[] values)
    {
        float sum = 0;
        for (int i = 0; i < values.Length; i++)
            sum += values[i];

        for (int i = 0; i < values.Length; i++)
            values[i] /= sum;
    }

    /// <summary>
    /// Return an array containing all available windows.
    /// Note that all windows returned will use the default constructor, but some
    /// windows have customization options in their constructors if you create them individually.
    /// </summary>
    public static IWindow[] GetWindows()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x.IsClass)
            .Where(x => !x.IsAbstract)
            .Where(x => x.GetInterfaces().Contains(typeof(IWindow)))
            .Select(x => (IWindow)Activator.CreateInstance(x))
            .ToArray();
    }
}

public class Blackman : Window, IWindow
{
    private readonly float A = 0.42659071f;
    private readonly float B = 0.49656062f;
    private readonly float C = 0.07684867f;

    public override string Name => "Blackman-Harris";
    public override string Description =>
        "The Blackman-Harris window is similar to Hamming and Hanning windows. " +
        "The resulting spectrum has a wide peak, but good side lobe compression.";

    public Blackman()
    {
    }

    //TODO: 5-term constructor to allow testing Python's flattop
    public Blackman(float a, float b, float c)
    {
        (A, B, C) = (a, b, c);
    }

    public override float[] Create(int size, bool normalize = false)
    {
        float[] window = new float[size];

        for (int i = 0; i < size; i++)
        {
            float frac = (float)i / (size - 1);
            window[i] = A - B * MathF.Cos(2 * MathF.PI * frac) + C * MathF.Cos(4 * MathF.PI * frac);
        }

        if (normalize)
            NormalizeInPlace(window);

        return window;
    }
}