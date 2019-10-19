/*
LM2.Revit.Utility provides commonly used methods.
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

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LM2.Revit
{
    public class Utility
    {
        public static XYZ GetCenter(Room room)
        {
            List<XYZ> endPointList = new List<XYZ>();


            IList<IList<BoundarySegment>> segments = room.GetBoundarySegments(new SpatialElementBoundaryOptions()
            {
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
            });

            if (null != segments)  //the room may not be bound
            {
                foreach (IList<BoundarySegment> segmentList in segments)
                {
                    foreach (BoundarySegment boundarySegment in segmentList)
                    {
                        //get start and end points of boudary segments
                        endPointList.Add(boundarySegment.GetCurve().GetEndPoint(0));
                        endPointList.Add(boundarySegment.GetCurve().GetEndPoint(1));
                    }
                }
            }

            if (endPointList.Count == 0)
            {
                return null;
            }

            double avgX = endPointList.Average((c) => c.X);
            double avgY = endPointList.Average((c) => c.Y);
            double avgZ = endPointList.Average((c) => c.Z);

            XYZ center = new XYZ(avgX, avgY, avgZ);

            if (room.IsPointInRoom(center))
            {
                return center;
            }

            center = new XYZ(avgX - 1, avgY, avgZ);

            if (room.IsPointInRoom(center))
            {
                return center;
            }

            center = new XYZ(avgX + 1, avgY, avgZ);

            if (room.IsPointInRoom(center))
            {
                return center;
            }

            center = new XYZ(avgX, avgY - 1, avgZ);

            if (room.IsPointInRoom(center))
            {
                return center;
            }

            center = new XYZ(avgX, avgY + 1, avgZ);

            if (room.IsPointInRoom(center))
            {
                return center;
            }

            throw new Exception(" Center not located in room");
        }
    }
}
