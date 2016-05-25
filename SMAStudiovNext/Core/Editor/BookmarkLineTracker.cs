using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace SMAStudiovNext.Core.Editor
{
    public class BookmarkLineTracker : ILineTracker
    {
        private readonly BookmarkManager _bookmarkManager;
        private readonly TextArea _textArea;
        private int _lineNumberRemoved;

        public BookmarkLineTracker(TextArea textArea, BookmarkManager bookmarkManager)
        {
            _bookmarkManager = bookmarkManager;
            _textArea = textArea;
        }

        public void BeforeRemoveLine(DocumentLine line)
        {
            _lineNumberRemoved = line.LineNumber;
        }

        public void SetLineLength(DocumentLine line, int newTotalLength)
        {
            //Console.WriteLine($"Line {line.LineNumber} length updated to: {newTotalLength} with new start offset: {line.Offset} and end offset: {line.EndOffset}");
        }

        public void LineInserted(DocumentLine insertionPos, DocumentLine newLine)
        {
            _bookmarkManager.AdjustLineOffsets(AdjustTypes.Added, newLine.LineNumber, 0);
            //Console.WriteLine($"Line inserted at {insertionPos.LineNumber} (new line number: {newLine.LineNumber}) with length: {newLine.Length} with start offset {newLine.Offset} and end offset: {newLine.EndOffset}");
        }

        public void RebuildDocument()
        {
        }

        public void ChangeComplete(DocumentChangeEventArgs e)
        {
            if (_lineNumberRemoved > -1)
            {
                _bookmarkManager.AdjustLineOffsets(AdjustTypes.Deleted, _lineNumberRemoved, e.RemovalLength);
                _lineNumberRemoved = -1;
            }

            _bookmarkManager.RecalculateOffsets(_textArea, BookmarkType.Breakpoint, 1);
        }
    }
}
