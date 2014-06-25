using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace SMAStudio.Editor
{
    public class MvvmTextEditor : TextEditor, INotifyPropertyChanged
    {
        public MvvmTextEditor()
        {
            Background = (Brush)new BrushConverter().ConvertFrom("#1e1e1e");
            Foreground = Brushes.White;
            ShowLineNumbers = true;

            // TODO: This doesn't follow the MVVM pattern and has to be changed
            TextArea.TextEntered += MainWindow.Instance.TextEntered;
            TextArea.TextEntering += MainWindow.Instance.TextEntering;
            // END TODO

            var dir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);

            if (File.Exists(dir + "/Xshd/SMA.xshd"))
            {
                using (XmlTextReader reader = new XmlTextReader(dir + "/Xshd/SMA.xshd"))
                {
                    SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }
        }

        public static DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(MvvmTextEditor),
                // binding changed callback: set value of underlying property
            new PropertyMetadata((obj, args) =>
            {
                MvvmTextEditor target = (MvvmTextEditor)obj;
                target.Text = (string)args.NewValue;
            })
        );

        public static DependencyProperty CaretOffsetProperty =
            DependencyProperty.Register("CaretOffset", typeof(int), typeof(MvvmTextEditor),
                // binding changed callback: set value of underlying property
            new PropertyMetadata((obj, args) =>
            {
                MvvmTextEditor target = (MvvmTextEditor)obj;
                target.CaretOffset = (int)args.NewValue;
            })
        );

        public new string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        public new int CaretOffset
        {
            get { return base.CaretOffset; }
            set { base.CaretOffset = value; }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
