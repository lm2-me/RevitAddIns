/*
LM2.Revit.StringHelpers provides commonly used methods for strings.
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

namespace LM2.Revit
{
    public class StringHelpers
    {
        public static string Angle2Cardinal(Document doc, double viewAngle)
        {
            double northOffset = Utility.GetDocNorthOffset(doc);
            double offsetAngle = Math.Round(viewAngle, 4) + northOffset;

            if (offsetAngle < 0)
            {
                offsetAngle += 360;
            }
            else if (offsetAngle > 360)
            {
                offsetAngle -= 360;
            }

            return offsetAngle >=   0 && offsetAngle <  45 ? "EAST" :
                   offsetAngle >=  45 && offsetAngle < 135 ? "NORTH" :
                   offsetAngle >= 135 && offsetAngle < 225 ? "WEST" :
                   offsetAngle >= 225 && offsetAngle < 315 ? "SOUTH" :
                   offsetAngle >= 315 && offsetAngle <= 360 ? "EAST" :
                   "";
        }
    }
}
