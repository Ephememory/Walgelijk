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

namespace Walgelijk.FFT;

public struct Complex
{
    public float Real;
    public float Imaginary;
    public float MagnitudeSquared { get { return Real * Real + Imaginary * Imaginary; } }
    public float Magnitude { get { return MathF.Sqrt(MagnitudeSquared); } }

    public Complex(float real, float imaginary)
    {
        Real = real;
        Imaginary = imaginary;
    }

    public override string ToString()
    {
        if (Imaginary < 0)
            return $"{Real}-{-Imaginary}j";
        else
            return $"{Real}+{Imaginary}j";
    }

    public static Complex operator +(Complex a, Complex b)
    {
        return new Complex(a.Real + b.Real, a.Imaginary + b.Imaginary);
    }

    public static Complex operator -(Complex a, Complex b)
    {
        return new Complex(a.Real - b.Real, a.Imaginary - b.Imaginary);
    }

    public static Complex operator *(Complex a, Complex b)
    {
        return new Complex(
            real: (a.Real * b.Real) - (a.Imaginary * b.Imaginary),
            imaginary: (a.Real * b.Imaginary) + (a.Imaginary * b.Real));
    }

    public static Complex operator *(Complex a, float b)
    {
        return new Complex(a.Real * b, a.Imaginary * b);
    }

    public static Complex[] FromReal(float[] real)
    {
        Complex[] complex = new Complex[real.Length];
        for (int i = 0; i < real.Length; i++)
            complex[i].Real = real[i];
        return complex;
    }

    public static float[] GetMagnitudes(Complex[] input)
    {
        float[] output = new float[input.Length];
        for (int i = 0; i < input.Length; i++)
            output[i] = input[i].Magnitude;
        return output;
    }
}