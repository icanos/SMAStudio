using Caliburn.Micro;
using ICSharpCode.AvalonEdit.Document;
using SMAStudiovNext.Modules.WindowConsole.Host;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SMAStudiovNext.Modules.WindowConsole.Views
{
    /// <summary>
    /// Interaction logic for VariableView.xaml
    /// </summary>
    public partial class ConsoleView : UserControl, IConsoleView
    {
        private Runspace _runspace;
        private CustomHost _host;
        private PowerShell _powershell;

        private object _lock = new object();

        public ConsoleView()
        {
            InitializeComponent();

            _host = new CustomHost(this);
            _runspace = RunspaceFactory.CreateRunspace(_host);
            _runspace.Open();

            lock (_lock)
            {
                _powershell = PowerShell.Create();
            }

            // TODO: Implement running shit!
        }
        
        public Coordinates GetCursorPosition()
        {
            var coordinates = default(Coordinates);

            Execute.OnUIThread(() =>
            {
                var line = console.Document.GetLineByOffset(console.CaretOffset);
                var lineOffsetX = console.CaretOffset - line.Offset;

                coordinates = new Coordinates(lineOffsetX, line.LineNumber);
            });
            
            return coordinates;
        }

        public void SetCursorPosition(Coordinates coordinates)
        {
            Execute.OnUIThread(() =>
            {
                var line = console.Document.GetLineByNumber(coordinates.Y);
                console.CaretOffset = line.Offset + coordinates.X;
            });
        }

        public string GetInput(int offset)
        {
            var line = default(DocumentLine);

            Execute.OnUIThread(() =>
            {
                line = console.Document.GetLineByOffset(console.CaretOffset);
            });

            if (line.Length == 0 && line.LineNumber > 0)
            {
                var lineContent = string.Empty;

                // We have pressed enter and the cursor is on a new line, get the previous line instead
                Execute.OnUIThread(() =>
                {
                    line = console.Document.GetLineByNumber(line.LineNumber - 1);
                    lineContent = console.Document.GetText(line);
                });

                
                return lineContent.Substring(offset);
            }
            else
                return string.Empty;
        }
        
        public int Write(string value)
        {
            var line = default(DocumentLine);
            var offset = 0;

            Execute.OnUIThread(() =>
            {
                console.Text += value;
                console.CaretOffset = console.Text.Length;

                line = console.Document.GetLineByOffset(console.CaretOffset);
                offset = console.CaretOffset - line.Offset;
            });

            return offset;
        }

        public int Write(string value, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
            // TODO: Implement colors!
            return Write(value);
        }
    }
}
