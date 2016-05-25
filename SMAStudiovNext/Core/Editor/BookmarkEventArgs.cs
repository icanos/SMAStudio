using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Core.Editor
{
    public class BookmarkEventArgs : EventArgs
    {
        public BookmarkEventArgs(Bookmark bookmark)
        {
            Bookmark = bookmark;
        }

        public BookmarkEventArgs(Bookmark bookmark, bool isDeleted)
        {
            Bookmark = bookmark;
            IsDeleted = isDeleted;
        }

        public Bookmark Bookmark { get; set; }

        public bool IsDeleted { get; set; }
    }
}
