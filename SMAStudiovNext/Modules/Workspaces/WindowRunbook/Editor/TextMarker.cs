using System;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor
{
    public class TextMarker : TextSegment
    {
        private readonly TextMarkerService _service;

        public TextMarker(TextMarkerService service, int startOffset, int length)
        {
            if (service == null)
                throw new ArgumentNullException("service");

            _service = service;
            StartOffset = startOffset;
            Length = length;
        }

        public event EventHandler Deleted;

        public void Delete()
        {
            _service.Remove(this);
        }

        internal void OnDeleted()
        {
            Deleted?.Invoke(this, EventArgs.Empty);
        }

        private void Redraw()
        {
            _service.Redraw(this);
        }

        private Color? _backgroundColor;

        public Color? BackgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                if (_backgroundColor != value)
                {
                    _backgroundColor = value;
                    Redraw();
                }
            }
        }

        private Color? _foregroundColor;

        public Color? ForegroundColor
        {
            get { return _foregroundColor; }
            set
            {
                if (_foregroundColor != value)
                {
                    _foregroundColor = value;
                    Redraw();
                }
            }
        }

        private FontWeight? _fontWeight;

        public FontWeight? FontWeight
        {
            get { return _fontWeight; }
            set
            {
                if (_fontWeight != value)
                {
                    _fontWeight = value;
                    Redraw();
                }
            }
        }

        private FontStyle? _fontStyle;

        public FontStyle? FontStyle
        {
            get { return _fontStyle; }
            set
            {
                if (_fontStyle != value)
                {
                    _fontStyle = value;
                    Redraw();
                }
            }
        }

        public object Tag { get; set; }

        private Color _markerColor;

        public Color MarkerColor
        {
            get { return _markerColor; }
            set
            {
                if (_markerColor != value)
                {
                    _markerColor = value;
                    Redraw();
                }
            }
        }

        private object _toolTip;
        public object ToolTip
        {
            get
            {
                if (_toolTip == null && Bookmark != null)
                    return Bookmark.Message;

                return _toolTip;
            }
            set { _toolTip = value; }
        }

        private Bookmark _bookmark;
        public Bookmark Bookmark
        {
            get { return _bookmark; }
            set
            {
                _bookmark = value;
            }
        }
    }
}
