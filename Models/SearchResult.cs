using System;

namespace WinFindGrep.Models
{
    public class SearchResult
    {
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
        public string LineContent { get; set; }
    }
}
