// Accord Wavelet Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

/// <summary>
///   Haar Wavelet Transform.
/// </summary>
/// 
/// <remarks>
/// <para>
///   References:
///   <list type="bullet">
///     <item><description>
///       Musawir Ali, An Introduction to Wavelets and the Haar Transform.
///       Available on: http://www.cs.ucf.edu/~mali/haar/ </description></item>
///   </list>
/// </para>
/// </remarks>
/// 
namespace Accord.Math.Wavelets
{
    public class Haar
    {
        private const float w0 = 0.5f;
        private const float w1 = -0.5f;
        private const float s0 = 0.5f;
        private const float s1 = 0.5f;

        /// <summary>
        ///   Discrete Haar Wavelet Transform
        /// </summary>
        /// 
        public static void FWT(float[] data)
        {
            float[] temp = new float[data.Length];

            int h = data.Length >> 1;
            for (int i = 0; i < h; i++)
            {
                int k = (i << 1);
                temp[i] = data[k] * s0 + data[k + 1] * s1;
                temp[i + h] = data[k] * w0 + data[k + 1] * w1;
            }

            for (int i = 0; i < data.Length; i++)
                data[i] = temp[i];
        }

        /// <summary>
        ///   Inverse Haar Wavelet Transform
        /// </summary>
        /// 
        public static void IWT(float[] data)
        {
            float[] temp = new float[data.Length];

            int h = data.Length >> 1;
            for (int i = 0; i < h; i++)
            {
                int k = (i << 1);
                temp[k] = (data[i] * s0 + data[i + h] * w0) / w0;
                temp[k + 1] = (data[i] * s1 + data[i + h] * w1) / s0;
            }

            for (int i = 0; i < data.Length; i++)
                data[i] = temp[i];
        }


        /// <summary>
        ///   Discrete Haar Wavelet 2D Transform
        /// </summary>
        /// 
        public static void FWT(float[,] data, int iterations)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            float[] row;
            float[] col;

            for (int k = 0; k < iterations; k++)
            {
                int lev = 1 << k;

                int levCols = cols / lev;
                int levRows = rows / lev;

                row = new float[levCols];
                for (int i = 0; i < levRows; i++)
                {
                    for (int j = 0; j < row.Length; j++)
                        row[j] = data[i, j];

                    FWT(row);

                    for (int j = 0; j < row.Length; j++)
                        data[i, j] = row[j];
                }


                col = new float[levRows];
                for (int j = 0; j < levCols; j++)
                {
                    for (int i = 0; i < col.Length; i++)
                        col[i] = data[i, j];

                    FWT(col);

                    for (int i = 0; i < col.Length; i++)
                        data[i, j] = col[i];
                }
            }
        }

        /// <summary>
        ///   Inverse Haar Wavelet 2D Transform
        /// </summary>
        /// 
        public static void IWT(float[,] data, int iterations)
        {
            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            float[] col;
            float[] row;

            for (int k = iterations - 1; k >= 0; k--)
            {
                int lev = 1 << k;

                int levCols = cols / lev;
                int levRows = rows / lev;

                col = new float[levRows];
                for (int j = 0; j < levCols; j++)
                {
                    for (int i = 0; i < col.Length; i++)
                        col[i] = data[i, j];

                    IWT(col);

                    for (int i = 0; i < col.Length; i++)
                        data[i, j] = col[i];
                }

                row = new float[levCols];
                for (int i = 0; i < levRows; i++)
                {
                    for (int j = 0; j < row.Length; j++)
                        row[j] = data[i, j];

                    IWT(row);

                    for (int j = 0; j < row.Length; j++)
                        data[i, j] = row[j];
                }
            }
        }
    }
}
