using Caliburn.Micro;
using Gemini.Modules.ErrorList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SMAStudiovNext.Core.Editor
{
    public enum BookmarkType
    {
        Breakpoint,
        CurrentDebugPoint,
        ParseError,
        AnalyzerWarning,
        AnalyzerInfo
    }

    public class Bookmark : IDisposable
    {
        private readonly IErrorList _errorList;
        private readonly TextMarkerService _textMarkerService;
        private readonly object _lock = new object();

        public Bookmark(BookmarkType bookmarkType, int lineNumber)
        {
            BookmarkType = bookmarkType;
            LineNumber = lineNumber;

            _errorList = IoC.Get<IErrorList>();
        }

        public Bookmark(BookmarkType bookmarkType, TextMarkerService textMarkerService, int lineNumber, int column, int startOffset, int length, string message, string fileName)
        {
            BookmarkType = bookmarkType;
            LineNumber = lineNumber;
            Message = message;

            _textMarkerService = textMarkerService;
            _errorList = IoC.Get<IErrorList>();

            InitializeBookmark(textMarkerService, lineNumber, column, startOffset, length, message, fileName);
        }

        private void InitializeBookmark(TextMarkerService textMarkerService, int lineNumber, int column, int startOffset, int length, string message, string fileName)
        {
            // Create a text marker
            OffsetInLine = column;
            TextMarker = textMarkerService.TryCreate(startOffset, length);

            if (TextMarker != null)
            {
                switch (BookmarkType)
                {
                    case BookmarkType.AnalyzerInfo:
                        TextMarker.MarkerColor = Colors.AliceBlue;
                        break;
                    case BookmarkType.AnalyzerWarning:
                        TextMarker.MarkerColor = Colors.DarkGoldenrod;
                        break;
                }
            }

            var errorListItemType = default(ErrorListItemType);

            switch (BookmarkType)
            {
                case BookmarkType.AnalyzerInfo:
                    errorListItemType = ErrorListItemType.Message;
                    break;
                case BookmarkType.AnalyzerWarning:
                    errorListItemType = ErrorListItemType.Warning;
                    break;
            }

            // Create an error list item
            Execute.OnUIThread(() =>
            {
                lock (_lock)
                {
                    ErrorListItem = new ErrorListItem(errorListItemType, _errorList.Items.Count, message, fileName, lineNumber, column);
                    _errorList.Items.Add(ErrorListItem);
                }
            });
        }

        public void CleanUp()
        {
            if (ErrorListItem != null)
            {
                _errorList.Items.Remove(ErrorListItem);
                _errorList.NotifyOfPropertyChange("Items");
                _errorList.NotifyOfPropertyChange("FilteredItems");
                _errorList.NotifyOfPropertyChange("ErrorItemCount");
                _errorList.NotifyOfPropertyChange("WarningItemCount");
                _errorList.NotifyOfPropertyChange("MessageItemCount");
            }

            if (_textMarkerService != null && TextMarker != null)
                _textMarkerService.Remove(TextMarker);
        }

        /// <summary>
        /// Gets or sets what type of bookmark this is.
        /// </summary>
        public BookmarkType BookmarkType { get; set; }

        /// <summary>
        /// Gets or sets which line number this bookmark is connected to.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Offset in the current line where the bookmark starts
        /// </summary>
        public int OffsetInLine { get; set; }

        /// <summary>
        /// Gets or sets the message connected to this bookmark.
        /// </summary>
        public string Message { get; set; }

        private TextMarker _textMarker;
        /// <summary>
        /// Gets or sets the text marker connected to the bookmark.
        /// </summary>
        public TextMarker TextMarker
        {
            get { return _textMarker; }
            set
            {
                _textMarker = value;

                if (_textMarker != null)
                    _textMarker.Bookmark = this;
            }
        }

        /// <summary>
        /// Gets or sets the error list item to the bookmark.
        /// </summary>
        public ErrorListItem ErrorListItem { get; set; }

        public override bool Equals(object obj)
        {
            var bookmark = obj as Bookmark;
            return bookmark != null && (bookmark.LineNumber == LineNumber && bookmark.BookmarkType == BookmarkType);
        }

        public override int GetHashCode()
        {
            return BookmarkType.GetHashCode() ^ LineNumber.GetHashCode();
        }

        public void Dispose()
        {
            CleanUp();
        }
    }
}
