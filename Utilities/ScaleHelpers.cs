using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LM2.Revit
{
    public static class ScaleHelpers
    {
        public static readonly List<KeyValuePair<string, int>> ScaleList = new List<KeyValuePair<string, int>>()
        {
            new KeyValuePair<string, int>( "12\" = 1'-0\"", 1 ),
            new KeyValuePair<string, int>( "6\" = 1'-0\"", 2 ),
            new KeyValuePair<string, int>( "3\" = 1'-0\"", 4 ),
            new KeyValuePair<string, int>( "1 1/2\" = 1'-0\"", 8 ),
            new KeyValuePair<string, int>( "1\" = 1'-0\"", 12 ),
            new KeyValuePair<string, int>( "3/4\" = 1'-0\"", 16 ),
            new KeyValuePair<string, int>( "1/2\" = 1'-0\"", 24 ),
            new KeyValuePair<string, int>( "3/8\" = 1'-0\"", 32 ),
            new KeyValuePair<string, int>( "1/4\" = 1'-0\"", 48 ),
            new KeyValuePair<string, int>( "3/16\" = 1'-0\"", 64 ),
            new KeyValuePair<string, int>( "1/8\" = 1'-0\"", 96 ),
            new KeyValuePair<string, int>( "1\" = 10'-0\"", 120 ),
            new KeyValuePair<string, int>( "3/32\" = 1'-0\"", 128 ),
            new KeyValuePair<string, int>( "1/16\" = 1'-0\"", 192 ),
            new KeyValuePair<string, int>( "1\" = 20'-0\"", 240 ),
            new KeyValuePair<string, int>( "3/64\" = 1'-0\"", 256 ),
            new KeyValuePair<string, int>( "1\" = 30'-0\"", 360 ),
            new KeyValuePair<string, int>( "1/32\" = 1'-0\"", 384 ),
            new KeyValuePair<string, int>( "1\" = 40'-0\"", 480 ),
            new KeyValuePair<string, int>( "1\" = 50'-0\"", 600 ),
            new KeyValuePair<string, int>( "1\" = 60'-0\"", 720 ),
            new KeyValuePair<string, int>( "1/64\" = 1'-0\"", 768 ),
            new KeyValuePair<string, int>( "1\" = 80'-0\"", 960 ),
            new KeyValuePair<string, int>( "1\" = 100'-0\"", 1200 ),
            new KeyValuePair<string, int>( "1\" = 160'-0\"", 1920 ),
            new KeyValuePair<string, int>( "1\" = 200'-0\"", 2400 ),
            new KeyValuePair<string, int>( "1\" = 300'-0\"", 3600 ),
            new KeyValuePair<string, int>( "1\" = 400'-0\"", 4800 )
        };
    }
}
