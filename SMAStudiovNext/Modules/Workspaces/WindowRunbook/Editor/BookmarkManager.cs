using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Debugging;
using ICSharpCode.AvalonEdit.Editing;
using Caliburn.Micro;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor
{
    public enum AdjustTypes
    {
        Deleted,
        Added
    }

    public class BookmarkManager
    {
        private readonly ObservableCollection<Bookmark> _bookmarks;
        private readonly TextMarkerService _textMarkerService;

        public event EventHandler<EventArgs> OnRedrawRequested;
        public event EventHandler<BookmarkEventArgs> OnBookmarkUpdated;

        public BookmarkManager(TextMarkerService textMarkerService)
        {
            _bookmarks = new ObservableCollection<Bookmark>();
            _textMarkerService = textMarkerService;
        }

        public void ClearErrorsAndWarnings()
        {
            foreach (var bookmark in _bookmarks.ToList())
            {
                bookmark.CleanUp();
                _bookmarks.Remove(bookmark);
            }
        }

        public void AdjustLineOffsets(AdjustTypes adjustType, int lineNumber, int offsetToAdd)
        {
            switch (adjustType)
            {
                case AdjustTypes.Added:
                    AdjustLineAdd(lineNumber, offsetToAdd);
                    break;
                case AdjustTypes.Deleted:
                    AdjustLineDelete(lineNumber, offsetToAdd);
                    break;
            }
        }

        public void RecalculateOffsets(TextArea textArea, BookmarkType bookmarkType, int lineNumberOrCaretOffset, int textLength = 1)
        {
            switch (bookmarkType)
            {
                case BookmarkType.Breakpoint:
                    // lineNumberOrCaretOffset: This will be a line number since breakpoints only work on whole lines

                    foreach (var marker in _bookmarks.Where(item => item.LineNumber >= lineNumberOrCaretOffset).ToList())
                    {
                        if (marker.TextMarker == null)
                        {
                            _bookmarks.Remove(marker);
                            continue;
                        }

                        var line = textArea.Document.GetLineByNumber(marker.LineNumber);
                        marker.TextMarker.StartOffset = line.Offset;
                        marker.TextMarker.Length = line.Length;
                    }
                    break;
                case BookmarkType.AnalyzerInfo:
                case BookmarkType.AnalyzerWarning:
                case BookmarkType.ParseError:
                    // lineNumberOrCaretOffset: This will be the caret offset
                    foreach (var marker in _bookmarks.Where(item => item.TextMarker.StartOffset >= lineNumberOrCaretOffset && item.BookmarkType == bookmarkType))
                    {
                        if (marker.TextMarker == null)
                        {
                            _bookmarks.Remove(marker);
                            continue;
                        }

                        marker.TextMarker.StartOffset += textLength;
                    }
                    break;
                default:
                    break;
            }

            Execute.OnUIThread(() =>
            {
                textArea.TextView.InvalidateLayer(ICSharpCode.AvalonEdit.Rendering.KnownLayer.Selection);
            });
        }

        public void RecalculateOffsets(TextArea textArea, IEnumerable<BookmarkType> bookmarkTypes, int lineNumberOrCaretOffset, int textLength = 1)
        {
            foreach (var bookmarkType in bookmarkTypes)
            {
                RecalculateOffsets(textArea, bookmarkType, lineNumberOrCaretOffset, textLength);
            }
        }

        private void AdjustLineAdd(int lineNumber, int offsetToAdd)
        {
            var lines = _bookmarks.Where(item => item.LineNumber >= lineNumber).ToList();

            foreach (var line in lines)
            {
                line.LineNumber++;
                OnBookmarkUpdated?.Invoke(this, new BookmarkEventArgs(line));
            }
        }

        private void AdjustLineDelete(int lineNumber, int offsetToRemove)
        {
            // Retrieve any lines that is found on the line that is to be deleted
            var lines = _bookmarks.Where(item => item.LineNumber == lineNumber).ToList();
            foreach (var line in lines)
            {
                line.CleanUp();
                _bookmarks.Remove(line);
                OnBookmarkUpdated?.Invoke(this, new BookmarkEventArgs(line, true));
            }

            lines = _bookmarks.Where(item => item.LineNumber > lineNumber).ToList();
            foreach (var line in lines)
            {
                line.LineNumber--;
                OnBookmarkUpdated?.Invoke(this, new BookmarkEventArgs(line));
            }
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
            bookmark.CleanUp();
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
                bookmark.CleanUp();
                _bookmarks.Remove(bookmark);
                OnBookmarkUpdated?.Invoke(this, new BookmarkEventArgs(bookmark, true));
            }

            OnRedrawRequested?.Invoke(this, new EventArgs());
        }

        public ObservableCollection<Bookmark> Bookmarks
        {
            get { return _bookmarks; }
        }

        public static bool IsLineBookmark(Bookmark bookmark)
        {
            return bookmark != null && (bookmark.BookmarkType == BookmarkType.Breakpoint || bookmark.BookmarkType == BookmarkType.CurrentDebugPoint);
        }
    }
}
