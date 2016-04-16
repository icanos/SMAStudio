using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Renderers;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor
{
    public class IconBarMargin : AbstractMargin, IDisposable
    {
        private readonly BookmarkManager _manager;

        public IconBarMargin(BookmarkManager manager)
        {
            _manager = manager;
            Margin = new Thickness(0, 0, 10.0, 0);
        }

        public virtual void Dispose()
        {
            TextView = null; // detach from TextView (will also detach from manager)
        }

        protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
        {
            if (oldTextView != null)
            {
                oldTextView.VisualLinesChanged -= OnRedrawRequested;
                _manager.OnRedrawRequested -= OnRedrawRequested;
                //oldTextView.MouseMove -= TextViewMouseMove;
            }

            base.OnTextViewChanged(oldTextView, newTextView);

            if (newTextView != null)
            {
                newTextView.VisualLinesChanged += OnRedrawRequested;
                _manager.OnRedrawRequested += OnRedrawRequested;
                //newTextView.MouseMove -= TextViewMouseMove;
            }

            InvalidateVisual();
        }

        private void OnRedrawRequested(object sender, EventArgs e)
        {
            // Don't invalidate the IconBarMargin if it'll be invalidated again once the
            // visual lines become valid.
            if (TextView != null && TextView.VisualLinesValid)
            {
                InvalidateVisual();
            }
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            // accept clicks even when clicking on the background
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(20, 0);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var renderSize = RenderSize;
            drawingContext.DrawRectangle(SystemColors.ControlBrush, null,
                new Rect(0, 0, renderSize.Width, renderSize.Height));
            drawingContext.DrawLine(new Pen(SystemColors.ControlDarkBrush, 1),
                new Point(renderSize.Width - 0.5, 0),
                new Point(renderSize.Width - 0.5, renderSize.Height));

            var textView = TextView;
            if (textView == null || !textView.VisualLinesValid)
                return;

            var pixelSize = PixelSnapHelpers.GetPixelSize(this);
            foreach (var line in textView.VisualLines)
            {
                var lineNumber = line.FirstDocumentLine.LineNumber;
                /*var bookmark = _manager.Bookmarks.FirstOrDefault(item => item.LineNumber == lineNumber && (item.BookmarkType == BookmarkType.Breakpoint || item.BookmarkType == BookmarkType.CurrentDebugPoint));

                if (bookmark == null)
                    continue;

                */
                var bookmark = _manager.Bookmarks.FirstOrDefault(item => item.LineNumber == lineNumber);
                var renderer = default(IMarkerRenderer);

                if (bookmark == null)
                    continue;

                switch (bookmark.BookmarkType)
                {
                    case BookmarkType.CurrentDebugPoint:
                    case BookmarkType.Breakpoint:
                        renderer = new DebugMarkerRenderer(textView, this);
                        break;
                    case BookmarkType.ParseError:
                        renderer = new ParseErrorRenderer(textView, this);
                        break;
                    case BookmarkType.AnalyzerWarning:
                        renderer = new AnalyzerWarningRenderer(textView, this);
                        break;
                }

                renderer?.Render(drawingContext, line, pixelSize);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            var line = GetLineFromMousePosition(e);
            if (!_manager.Add(new Bookmark(BookmarkType.Breakpoint, line)))
            {
                _manager.RemoveAt(BookmarkType.Breakpoint, line);
            }
        }

        private int GetLineFromMousePosition(MouseEventArgs e)
        {
            var textView = TextView;

            var vl = textView?.GetVisualLineFromVisualTop(e.GetPosition(textView).Y + textView.ScrollOffset.Y);
            if (vl == null)
                return 0;

            return vl.FirstDocumentLine.LineNumber;
        }
    }
}