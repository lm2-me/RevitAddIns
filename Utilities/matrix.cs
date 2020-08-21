/*
LM2.Revit.Matrix provides commonly used methods for matrix operations.
Copyright(C) 2019  Lisa-Marie Mueller

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see<https://www.gnu.org/licenses/> 
*/

using Autodesk.Revit.DB;
using System;
using System.Linq;

//set up docstrings for all methods
namespace LM2.Revit
{
    /// <summary>
    /// Methods used for Matrix operations.
    /// </summary>
    public class Matrix
    {
        /// <summary>
        /// Prints matrices with attached debug messages
        /// </summary>
        /// <param name="msg">String to print</param>
        /// <param name="m">Matrix to Print</param>
        /// <param name="Debug"></param>
        public static void print(string msg, double[][] m, Action<string> Debug)
        {
            Debug(msg + ": [" + String.Join(
                ",",
                m.Select(
                    r => "(" + String.Join(",", r) + ")"
                )
            ) + "]");
        }

        /// <summary>
        /// Matrix for rotation around the X Axis.
        /// </summary>
        /// <param name="t">angle to rotate by in radians</param>
        /// <returns>rotation matrix</returns>
        public static double[][] XAxisRotation(double t)
        {
            return new [] {
                new[] { 1.0, 0.0, 0.0 },
                new[] { 0.0, Math.Cos(t), - Math.Sin(t) },
                new[] { 0.0, Math.Sin(t), Math.Cos(t) }
             };
        }

        /// <summary>
        /// Matrix for rotation around the Y Axis.
        /// </summary>
        /// <param name="t">angle to rotate by in radians</param>
        /// <returns>rotation matrix</returns>
        public static double[][] YAxisRotation(double t)
        {
            return new[] {
                new[] { Math.Cos(t), 0.0, Math.Sin(t) },
                new[] { 0.0, 1.0, 0.0 },
                new[] { -Math.Sin(t), 0.0, Math.Cos(t) }
             };
        }

        /// <summary>
        /// Matrix for rotation around the Z Axis. Assumes positive rotation is counterclockwise.
        /// </summary>
        /// <param name="t">angle to rotate by in radians</param>
        /// <returns>rotation matrix</returns>
        public static double[][] ZAxisRotation(double t)
        {
            return new[] {
                new[] { Math.Cos(t), -Math.Sin(t), 0.0 },
                new[] { Math.Sin(t), Math.Cos(t), 0.0 },
                new[] { 0.0, 0.0, 1.0 }
             };
        }

        /// <summary>
        /// Gets the minors for the matrix. This is an intermediary step to finding the inverse of a matrix.
        /// </summary>
        /// <param name="m"> The matrix to get the minors for</param>
        /// <returns>array of minors</returns>
        public static double[][] getMinors(double[][] m)
        {
            return new[] {
                new[] { m[1][1] * m[2][2] - m[1][2] * m[2][1], m[1][0] * m[2][2] - m[1][2] * m[2][0], m[1][0] * m[2][1] - m[1][1] * m[2][0] },
                new[] { m[0][1] * m[2][2] - m[0][2] * m[2][1], m[0][0] * m[2][2] - m[0][2] * m[2][0], m[0][0] * m[2][1] - m[0][1] * m[2][0] },
                new[] { m[0][1] * m[1][2] - m[0][2] * m[1][1], m[0][0] * m[1][2] - m[0][2] * m[1][0], m[0][0] * m[1][1] - m[0][1] * m[1][0] }
             };
        }

        /// <summary>
        /// Gets cofactors for the matrix. This is an intermediary step to finding the inverse of a matrix.
        /// </summary>
        /// <param name="m">The matrix to get the cofactors for</param>
        /// <returns>array of cofactors</returns>
        public static double[][] getCofactors(double[][] m)
        {
            return new[] {
                new[] {  m[0][0], -m[0][1],  m[0][2] },
                new[] { -m[1][0],  m[1][1], -m[1][2] },
                new[] {  m[2][0], -m[2][1],  m[2][2] }
            };
        }

        /// <summary>
        /// Gets adjugates for the matrix. This is an intermediary step to finding the inverse of a matrix.
        /// </summary>
        /// <param name="m">The matrix to get the adjugates for</param>
        /// <returns>array of adjugates</returns>
        public static double[][] getAdjugate(double[][] m)
        {
            return new[] {
                new[] { m[0][0], m[1][0], m[2][0] },
                new[] { m[0][1], m[1][1], m[2][1] },
                new[] { m[0][2], m[1][2], m[2][2] }
            };
        }

        /// <summary>
        /// Gets the determinants for the matrix. This is an intermediary step to finding the inverse of a matrix.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static double getDeterminant(double[][] m, double[][] c)
        {
            return m[0][0] * c[0][0] + m[0][1] * c[0][1] + m[0][2] * c[0][2];
        }

        /// <summary>
        /// scales a matrix
        /// </summary>
        /// <param name="m">matrix to scale</param>
        /// <param name="s">scale factor</param>
        /// <returns>scaled matrix</returns>
        public static double[][] scale(double[][] m, double s)
        {
            return new[] {
                new[] { m[0][0] * s, m[0][1] * s, m[0][2] * s },
                new[] { m[1][0] * s, m[1][1] * s, m[1][2] * s },
                new[] { m[2][0] * s, m[2][1] * s, m[2][2] * s }
            };
        }

        /// <summary>
        /// Inverts a matrix
        /// </summary>
        /// <param name="m">matrix to invert</param>
        /// <returns>inverted matrix</returns>
        public static double[][] invert(double[][] m)
        {
            var minors = getMinors(m);
            var cofactors = getCofactors(minors);
            var ajugate = getAdjugate(cofactors);
            var det = getDeterminant(m, cofactors);
            if (det == 0)
            {
                throw new Exception("Cannot invert matrix");
            }
            return scale(ajugate, 1 / det);
        }

        /// <summary>
        /// Creates a matrix of the Revit transform value.
        /// </summary>
        /// <param name="t">Revit transform value</param>
        /// <returns>Revit transform as matrix</returns>
        public static double[][] transform2matrix(Transform t)
        {
            return new []
            {
                new[] { t.BasisX.X, t.BasisX.Y, t.BasisX.Z },
                new[] { t.BasisY.X, t.BasisY.Y, t.BasisY.Z },
                new[] { t.BasisZ.X, t.BasisZ.Y, t.BasisZ.Z }
            };
        }

        /// <summary>
        /// Converts an XYZ coordinate to a matrix
        /// </summary>
        /// <param name="p">XYZ coordinate to return as matrix</param>
        /// <returns>matrix of XYZ coordinate</returns>
        public static double[] xyz2matrix(XYZ p)
        {
            return new[]
            {
                p.X,
                p.Y,
                p.Z
            };
        }

        /// <summary>
        /// Converts a matrix into an XYZ coordinate
        /// </summary>
        /// <param name="p">The matrix to return as XYZ coordinate</param>
        /// <returns>XYZ coordinate of matrix</returns>
        public static XYZ matrix2xyz(double[] p)
        {
            return new XYZ(p[0], p[1], p[2]);
        }

        /// <summary>
        /// Finds the dot prodcut of two matrices
        /// </summary>
        /// <param name="m1">The first matrix for the dot product</param>
        /// <param name="m2T">The second matrix for the dot product</param>
        /// <returns>the dot product</returns>
        public static double[] dot(double[][] m1, double[] m2T)
        {
            return new[]
            {
                m1[0][0] * m2T[0] + m1[0][1] * m2T[1] + m1[0][2] * m2T[2],
                m1[1][0] * m2T[0] + m1[1][1] * m2T[1] + m1[1][2] * m2T[2],
                m1[2][0] * m2T[0] + m1[2][1] * m2T[1] + m1[2][2] * m2T[2],
            };
        }
    }
}
