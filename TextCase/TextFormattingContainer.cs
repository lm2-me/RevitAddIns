/*
LM2.Revit.TextFormattingContainer formats text to save to a text file.
Copyright(C) 2020  Lisa-Marie Mueller

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
    [Serializable]
    public class TextFormattingContainer
    {
        public int textNoteTypeID { get; set; }
        public bool textNoteAllCapsStatus { get; set; }
        public FormatStatus textNoteBoldStatus { get; set; }
        public FormatStatus textNoteItalicsStatus { get; set; }
        public FormatStatus textNoteUnderlineStatus { get; set; }
    }
}
