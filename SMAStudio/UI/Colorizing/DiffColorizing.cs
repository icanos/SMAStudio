using DiffPlex.DiffBuilder.Model;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SMAStudio.UI.Colorizing
{
    public class DiffColorizing : DocumentColorizingTransformer
    {
        private SideBySideDiffModel _edits;
        private bool _leftSide;
 
        public DiffColorizing(SideBySideDiffModel edits, bool leftSide)
        {
            _edits = edits;
            _leftSide = leftSide;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (_leftSide)
            {
                InternalColorizeLine(line, _edits.OldText.Lines);
            }
            else
            {
                InternalColorizeLine(line, _edits.NewText.Lines);
            }
        }

        private void InternalColorizeLine(DocumentLine line, List<DiffPiece> pieces)
        {
            if ((line.LineNumber - 1) >= pieces.Count)
                return;

            //foreach (var edit in pieces)
            var edit = pieces[line.LineNumber - 1];

            //if (edit.Position < line.Offset)
            //  continue;

            //if (edit.Type != DiffPlex.DiffBuilder.Model.ChangeType.Imaginary && edit.Position + edit.Text.Length > line.EndOffset)
            //    continue;

            if (edit.Type == DiffPlex.DiffBuilder.Model.ChangeType.Deleted)
            {
                base.ChangeLinePart(
                    line.Offset,
                    line.EndOffset,
                    (VisualLineElement element) =>
                    {
                        element.BackgroundBrush = Brushes.Red;
                    });
            }
            else if (edit.Type == DiffPlex.DiffBuilder.Model.ChangeType.Inserted)
            {
                base.ChangeLinePart(
                    line.Offset,
                    line.EndOffset,
                    (VisualLineElement element) =>
                    {
                        element.BackgroundBrush = Brushes.Green;
                    });
            }
            else if (edit.Type == DiffPlex.DiffBuilder.Model.ChangeType.Modified)
            {
                foreach (var subPart in edit.SubPieces)
                {
                    if (subPart.Type == DiffPlex.DiffBuilder.Model.ChangeType.Imaginary ||
                        subPart.Type == ChangeType.Unchanged)
                        continue;

                    Brush brush = Brushes.Gray;

                    if (subPart.Type == ChangeType.Inserted)
                        brush = Brushes.Green;
                    else if (subPart.Type == ChangeType.Deleted)
                        brush = Brushes.Red;

                    int position = CurrentContext.Document.GetText(line).IndexOf(subPart.Text);
                    int startOffset = line.Offset + position;
                    int endOffset = startOffset + subPart.Text.Length;

                    base.ChangeLinePart(
                        startOffset,
                        endOffset,
                        (VisualLineElement element) =>
                        {
                            element.BackgroundBrush = brush;
                        });
                }
            }
            else if (edit.Type == DiffPlex.DiffBuilder.Model.ChangeType.Imaginary)
            {
                /*base.ChangeLinePart(
                        line.Offset,
                        line.EndOffset,
                        (VisualLineElement element) =>
                        {
                            element.BackgroundBrush = Brushes.LightGray;
                        });*/
            }
        }
    }
}
