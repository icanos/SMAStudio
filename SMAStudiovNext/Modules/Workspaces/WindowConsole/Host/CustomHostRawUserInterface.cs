using SMAStudiovNext.Modules.WindowConsole.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Host;
using System.Text;
using System.Threading.Tasks;

namespace SMAStudiovNext.Modules.WindowConsole.Host
{
    internal class CustomHostRawUserInterface : PSHostRawUserInterface
    {
        private readonly ConsoleView _consoleView;

        //private const int DefaultConsoleHeight = 100;
        //private const int DefaultConsoleWidth = 120;

        private Size currentBufferSize;// = new Size(DefaultConsoleWidth, DefaultConsoleHeight);

        public CustomHostRawUserInterface(ConsoleView consoleView)
        {
            _consoleView = consoleView;
            currentBufferSize = new Size((int)_consoleView.Width, (int)_consoleView.Height);
        }

        public override ConsoleColor BackgroundColor
        {
            get; set;
        }

        public override Size BufferSize
        {
            get { return currentBufferSize; }
            set { currentBufferSize = value; }
        }

        /// <summary>
        /// TODO: Implement this when we have somewhat of a working console
        /// </summary>
        public override Coordinates CursorPosition
        {
            get { return _consoleView.GetCursorPosition(); }
            set { _consoleView.SetCursorPosition(value); }
        }

        /// <summary>
        /// Gets or sets the cursor size. This implementation maps to the 
        /// Console.CursorSize property.
        /// </summary>
        public override int CursorSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the foreground color of the text to be written.
        /// This implementation maps to the Console.ForegroundColor 
        /// property.
        /// </summary>
        public override ConsoleColor ForegroundColor
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value that indicates whether a key is available. 
        /// This implementation maps to the Console.KeyAvailable property.
        /// </summary>
        public override bool KeyAvailable
        {
            get { return false; }
        }

        public override Size MaxPhysicalWindowSize
        {
            get
            {
                return new Size((int)_consoleView.Width, (int)_consoleView.Height);
            }
        }

        public override Size MaxWindowSize
        {
            get
            {
                return new Size((int)_consoleView.Width, (int)_consoleView.Height);
            }
        }

        public override Coordinates WindowPosition
        {
            get;
            set;
        }

        public override Size WindowSize
        {
            get;
            set;
        }

        public override string WindowTitle
        {
            get { return "Console"; }
            set { }
        }

        public override void FlushInputBuffer()
        {
            // Currently does nothing
        }

        public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            throw new NotImplementedException();
        }

        public override KeyInfo ReadKey(ReadKeyOptions options)
        {
            throw new NotImplementedException();
        }

        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
            throw new NotImplementedException();
        }

        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            throw new NotImplementedException();
        }

        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            throw new NotImplementedException();
        }
    }
}
