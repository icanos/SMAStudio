using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor
{
    public enum BookmarkType
    {
        Breakpoint,
        CurrentDebugPoint,
        ParseError
    }

    public class Bookmark
    {
        public Bookmark(BookmarkType bookmarkType, int lineNumber)
        {
            BookmarkType = bookmarkType;
            LineNumber = lineNumber;
        }

        public BookmarkType BookmarkType { get; set; }

        public int LineNumber { get; set; }

        public override bool Equals(object obj)
        {
            var bookmark = obj as Bookmark;
            return bookmark != null && (bookmark.LineNumber == LineNumber && bookmark.BookmarkType == BookmarkType);
        }

        public override int GetHashCode()
        {
            return BookmarkType.GetHashCode() ^ LineNumber.GetHashCode();
        }
    }
}
