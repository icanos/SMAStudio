using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Debugging;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor
{
    public class BookmarkManager
    {
        private readonly ObservableCollection<Bookmark> _bookmarks;

        public event EventHandler<EventArgs> OnRedrawRequested;
        public event EventHandler<BookmarkEventArgs> OnBookmarkUpdated;

        public BookmarkManager()
        {
            _bookmarks = new ObservableCollection<Bookmark>();
        }

        public bool Add(Bookmark bookmark)
        {
            // Only support one bookmark per line
            if (_bookmarks.Contains(bookmark))
                return false;

            _bookmarks.Add(bookmark);

            OnRedrawRequested?.Invoke(this, new EventArgs());
            OnBookmarkUpdated?.Invoke(this, new BookmarkEventArgs(bookmark));

            return true;
        }

        public void Remove(Bookmark bookmark)
        {
            _bookmarks.Remove(bookmark);

            OnRedrawRequested?.Invoke(this, new EventArgs());
            OnBookmarkUpdated?.Invoke(this, new BookmarkEventArgs(bookmark, true));
        }

        public void RemoveAt(BookmarkType bookmarkType, int lineNumber)
        {
            var bookmarks =
                _bookmarks.Where(item => item.BookmarkType.Equals(bookmarkType) && item.LineNumber.Equals(lineNumber)).ToList();

            foreach (var bookmark in bookmarks)
            {
                _bookmarks.Remove(bookmark);
                OnBookmarkUpdated?.Invoke(this, new BookmarkEventArgs(bookmark, true));
            }

            OnRedrawRequested?.Invoke(this, new EventArgs());
        }

        public ObservableCollection<Bookmark> Bookmarks
        {
            get { return _bookmarks; }
        }
    }
}
