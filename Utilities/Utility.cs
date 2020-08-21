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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;

namespace LM2.Revit
{
    public class Utility
    {
        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// Gets the centroid of the room, if the centroid is not inside the room or on the boundary, it adjusts it to be in the room
        /// </summary>
        /// <param name="room"></param>
        /// <returns>Center of Room</returns>
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

        /// <summary>
        /// get view orientation if project north is rotated
        /// </summary>
        /// <param name="doc"></param>
        /// <returns>Angle between veiw and project</returns>
        public static double GetDocNorthOffset(Document doc)
        {
            ProjectLocationSet projLocationSet = doc.ProjectLocations;
            return doc.ProjectLocations
                    .Cast<ProjectLocation>()
                    .Where(loc => loc.Name == "Internal")
                    .Select(loc => loc.GetProjectPosition(XYZ.Zero))
                    .Select(position => Math.Round(Radians2Degrees(position.Angle), 3))
                    .FirstOrDefault();
        }
        
        /// <summary>
        /// Gets the angle in degrees of the vector
        /// </summary>
        /// <param name="vector">XYZ vector to convert to deg</param>
        /// <returns>Angle in Degrees</returns>
        public static double GetVectorAngle(XYZ vector)
        {
            double radians = Math.Atan2(vector.Y, vector.X);
            double radiansOffset = radians < Math.PI ? radians + Math.PI : radians - Math.PI;
            return Radians2Degrees(radiansOffset);

        }

        /// <summary>
        /// Converts radians to degrees
        /// </summary>
        /// <param name="radians">value of radians as double</param>
        /// <returns>value of degrees as double</returns>
        public static double Radians2Degrees(double radians)
        {
            return radians * (180 / Math.PI);
        }

        /// <summary>
        /// Converts degrees to radians
        /// </summary>
        /// <param name="degrees">value of degress as double</param>
        /// <returns>value of radians as double</returns>
        public double Degrees2Radians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        /// <summary>
        /// Compares the min and max values of an XYZ coordinate and ensure the min is smaller then the max, if not the values are flipped.
        /// </summary>
        /// <param name="min">the minimum XYZ</param>
        /// <param name="max">the maximum XYZ</param>
        /// <param name="newMin">returns the actual min value</param>
        /// <param name="newMax">returns the actual max value</param>
        public static void ReorderMinMax(XYZ min, XYZ max, out XYZ newMin, out XYZ newMax)
        {
            double newMinX = min.X < max.X ? min.X : max.X;
            double newMinY = min.Y < max.Y ? min.Y : max.Y;
            double newMinZ = min.Z < max.Z ? min.Z : max.Z;

            double newMaxX = min.X > max.X ? min.X : max.X;
            double newMaxY = min.Y > max.Y ? min.Y : max.Y;
            double newMaxZ = min.Z > max.Z ? min.Z : max.Z;

            newMin = new XYZ(newMinX, newMinY, newMinZ);
            newMax = new XYZ(newMaxX, newMaxY, newMaxZ);
        }

        /// <summary>
        /// Sends and formats telemetry data to URL in config file
        /// </summary>
        /// <param name="doc">the UI document</param>
        /// <param name="slackURL">the slack URL to post data to</param>
        /// <param name="pluginName">the name of the plug-in being run</param>
        /// <param name="message">the additional message to send</param>
        /// <returns>result</returns>
        public static string SendTelemetryData(Document doc, string slackURL, string pluginName, string message)
        {
            if (slackURL == null)
            {
                return null;
            }

            String userName = doc.Application.Username;
            String dateTime = DateTime.Now.ToLocalTime().ToString();
            String formattedMessage;

            formattedMessage = $"[{pluginName}] - User: {userName} - Time: {dateTime}\n{message}";

            StringContent content = new StringContent($"{{ \"text\": \"{formattedMessage}\" }}", Encoding.UTF8, "application/json");
            var result = client.PostAsync(slackURL, content).Result;

            return result.Content.ReadAsStringAsync().Result;
        }

        /// <summary>
        /// Reads the config file to find the URL to send the telemetry data to.
        /// </summary>
        /// <returns>config information</returns>
        public static Config ReadConfig()
        {
            string fileName = "config.xml";
            string fullFilePath = "C:\\ProgramData\\Autodesk\\Revit\\Addins\\2019" + "\\" + fileName;
            Config config = new Config();

            if (!File.Exists(fullFilePath))
            {
                return config;
            }
            string fileContent = File.ReadAllText(fullFilePath);

            XDocument configDoc = XDocument.Parse(fileContent);
            IEnumerable<XElement> configElements = configDoc.Descendants("config").Elements();

            foreach (XElement e in configElements)
            {
                if (e.Name == "telemetryURL")
                {
                    config.telemetryURL = e.Value;
                }
            }

            return config;

        }
    
        /// <summary>
        /// Find the min and max coordinates in a list of coordinates
        /// </summary>
        /// <param name="xyzList">the list of coordiantes of which to find the min and max points</param>
        /// <returns>the min and max points in a tuple; min at item1, max at item2</returns>
        public static Tuple<XYZ, XYZ> GetMinMaxFromList(List<XYZ> xyzList)
        {
            double? minX = null;
            double? minY = null;
            double? minZ = null;
            foreach (XYZ point in xyzList)
            {
                if (point.X < minX || minX == null)
                {
                    minX = point.X;
                }
                if (point.Y < minY || minY == null)
                {
                    minY = point.Y;
                }
                if (point.Z < minZ || minZ == null)
                {
                    minZ = point.Z;
                }
            }

            List<XYZ> xyzListAdjusted = new List<XYZ>();
            for (int i = 0; i < xyzList.Count; i++)
            {
                xyzListAdjusted.Add(new XYZ(
                    xyzList[i].X - minX.Value,
                    xyzList[i].Y - minY.Value,
                    xyzList[i].Z - minZ.Value
                ));
            }

            int minIndex = 0;
            int maxIndex = 0;
            double? minDistance = null;
            double? maxDistance = null;
            for (int i = 0; i < xyzListAdjusted.Count; i++)
            {
                // Euclidian distance from (0,0,0)
                double distance = Math.Sqrt(
                    Math.Pow(xyzListAdjusted[i].X, 2) +
                    Math.Pow(xyzListAdjusted[i].Y, 2) +
                    Math.Pow(xyzListAdjusted[i].Z, 2)
                );
                if (distance < minDistance || minDistance == null)
                {
                    minIndex = i;
                    minDistance = distance;
                }
                if (distance > maxDistance || maxDistance == null)
                {
                    maxIndex = i;
                    maxDistance = distance;
                }
            }

            Tuple <XYZ, XYZ> minMax = Tuple.Create(
                xyzList[minIndex],
                xyzList[maxIndex]
                );

            return minMax;
        }
    }
}
