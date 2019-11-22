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


namespace LM2.Revit
{
    public class Matrix
    {
        public static void print(string msg, double[][] m, Action<string> Debug)
        {
            Debug(msg + ": [" + String.Join(
                ",",
                m.Select(
                    r => "(" + String.Join(",", r) + ")"
                )
            ) + "]");
        }

        public static double[][] getMinors(double[][] m)
        {
            return new[] {
                new[] { m[1][1] * m[2][2] - m[1][2] * m[2][1], m[1][0] * m[2][2] - m[1][2] * m[2][0], m[1][0] * m[2][1] - m[1][1] * m[2][0] },
                new[] { m[0][1] * m[2][2] - m[0][2] * m[2][1], m[0][0] * m[2][2] - m[0][2] * m[2][0], m[0][0] * m[2][1] - m[0][1] * m[2][0] },
                new[] { m[0][1] * m[1][2] - m[0][2] * m[1][1], m[0][0] * m[1][2] - m[0][2] * m[1][0], m[0][0] * m[1][1] - m[0][1] * m[1][0] }
             };
        }

        public static double[][] getCofactors(double[][] m)
        {
            return new[] {
                new[] {  m[0][0], -m[0][1],  m[0][2] },
                new[] { -m[1][0],  m[1][1], -m[1][2] },
                new[] {  m[2][0], -m[2][1],  m[2][2] }
            };
        }

        public static double[][] getAdjugate(double[][] m)
        {
            return new[] {
                new[] { m[0][0], m[1][0], m[2][0] },
                new[] { m[0][1], m[1][1], m[2][1] },
                new[] { m[0][2], m[1][2], m[2][2] }
            };
        }

        public static double getDeterminant(double[][] m, double[][] c)
        {
            return m[0][0] * c[0][0] + m[0][1] * c[0][1] + m[0][2] * c[0][2];
        }

        public static double[][] scale(double[][] m, double s)
        {
            return new[] {
                new[] { m[0][0] * s, m[0][1] * s, m[0][2] * s },
                new[] { m[1][0] * s, m[1][1] * s, m[1][2] * s },
                new[] { m[2][0] * s, m[2][1] * s, m[2][2] * s }
            };
        }


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

        public static double[][] transform2matrix(Transform t)
        {
            return new []
            {
                new[] { t.BasisX.X, t.BasisX.Y, t.BasisX.Z },
                new[] { t.BasisY.X, t.BasisY.Y, t.BasisY.Z },
                new[] { t.BasisZ.X, t.BasisZ.Y, t.BasisZ.Z }
            };
        }

        public static double[] xyz2matrix(XYZ p)
        {
            return new[]
            {
                p.X,
                p.Y,
                p.Z
            };
        }

        public static XYZ matrix2xyz(double[] p)
        {
            return new XYZ(p[0], p[1], p[2]);
        }

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
