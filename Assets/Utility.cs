using System;
using UnityEngine;

public static class Utility
{
    public static Color RGBToYUV(Color rgb)
    {
        return new Color(RgbToY(rgb), RgbToU(rgb), RgbToV(rgb));
    }

    public static Color YUVToRGB(Color yuv)
    {
        return new Color(YuvToR(yuv), YuvToG(yuv), YuvToB(yuv));
    }

    public static float RgbToY(Color rgb)
    {
        return 0.299f * rgb.r + 0.587f * rgb.g + 0.114f * rgb.b;
    }

    public static float RgbToU(Color rgb)
    {
        return -0.147f * rgb.r - 0.289f * rgb.g + 0.436f * rgb.b;
    }

    public static float RgbToV(Color rgb)
    {
        return 0.615f * rgb.r - 0.515f * rgb.g - 0.100f * rgb.b;
    }

    public static float YuvToR(Color yuv)
    {
        return yuv.r + 1.140f * yuv.b;
    }

    public static float YuvToG(Color yuv)
    {
        return yuv.r - 0.395f * yuv.g - 0.581f * yuv.b;
    }

    public static float YuvToB(Color yuv)
    {
        return yuv.r + 2.032f * yuv.g;
    }

    /// <summary>
    /// Extract LL2 from DWTed data 
    /// </summary>
    public static T[,] LL2<T>(this T[,] dwtData)
    {
        var width = dwtData.GetUpperBound(0) + 1;
        var height = dwtData.GetUpperBound(1) + 1;

        return dwtData.Submatrix(0, width / 4 - 1, 0, height / 4 - 1);
    }

    /// <summary>
    ///   Returns a sub matrix extracted from the current matrix.
    /// </summary>
    /// 
    /// <param name="source">The matrix to return the submatrix from.</param>
    /// <param name="startRow">Start row index</param>
    /// <param name="endRow">End row index</param>
    /// <param name="startColumn">Start column index</param>
    /// <param name="endColumn">End column index</param>
    /// 
    public static T[,] Submatrix<T>(this T[,] source,
        int startRow, int endRow, int startColumn, int endColumn)
    {
        return submatrix(source, null, startRow, endRow, startColumn, endColumn);
    }

    /// <summary>
    ///   Extracts a selected area from a matrix.
    /// </summary>
    /// 
    /// <remarks>
    ///   Routine adapted from Lutz Roeder's Mapack for .NET, September 2000.
    /// </remarks>
    /// 
    private static T[,] submatrix<T>(this T[,] source, T[,] destination,
        int startRow, int endRow, int startColumn, int endColumn)
    {
        if (source == null)
            throw new ArgumentNullException("source");

        int rows = source.GetLength(0);
        int cols = source.GetLength(1);

        if ((startRow > endRow) || (startColumn > endColumn) || (startRow < 0) ||
            (startRow >= rows) || (endRow < 0) || (endRow >= rows) ||
            (startColumn < 0) || (startColumn >= cols) || (endColumn < 0) ||
            (endColumn >= cols))
        {
            throw new ArgumentException("Argument out of range.");
        }

        if (destination == null)
            destination = new T[endRow - startRow + 1, endColumn - startColumn + 1];

        for (int i = startRow; i <= endRow; i++)
            for (int j = startColumn; j <= endColumn; j++)
                destination[i - startRow, j - startColumn] = source[i, j];

        return destination;
    }
}
