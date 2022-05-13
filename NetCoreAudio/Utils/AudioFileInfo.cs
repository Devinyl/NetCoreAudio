using System;
using System.Collections.Generic;
using System.Text;

namespace NetCoreAudio.Utils
{
    public  class AudioFileInfo
    {
        public int Length { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileExtension { get; set; }
        public string Artist { get; set; }
        public string Title { get; set; }
        public string Album { get; set; }
        public string Genre { get; set; }
        public string Year { get; set; }
        public string Track { get; set; }
        public string Comment { get; set; }
        public long FileSize { get; internal set; }
    }
}
