using Caliburn.Micro;
using Gemini.Modules.StatusBar;
using System.Timers;
using System;

namespace SMAStudiovNext.Core
{
    public class StatusManager : IStatusManager, IDisposable
    {
        private readonly IStatusBar _statusBar;
        private readonly Timer _timeoutTimer;

        private string _cachedText = string.Empty;

        public StatusManager()
        {
            _statusBar = IoC.Get<IStatusBar>();
            _timeoutTimer = new Timer();
            _timeoutTimer.Elapsed += OnTimerExpired;
        }

        private void OnTimerExpired(object sender, ElapsedEventArgs e)
        {
            _timeoutTimer.Stop();

            if (_cachedText.Equals(GetText()))
                SetText("");
        }

        private string GetText()
        {
            var content = string.Empty;

            try
            {
                Execute.OnUIThread(() =>
                {
                    content = _statusBar.Items[0].Message;
                });
            }
            catch (Exception)
            {
                // Don't remember which error to capture ;-)
            }

            return content;
        }

        public void SetText(string message)
        {
            try
            {
                Execute.OnUIThread(() =>
                {
                    _statusBar.Items[0].Message = message;
                    _cachedText = message;
                });
            }
            catch (Exception)
            {

            }
        }

        public void SetTimeoutText(string message, int timeoutInSeconds)
        {
            SetText(message);

            _timeoutTimer.Interval = timeoutInSeconds * 1000;
            _timeoutTimer.Start();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _timeoutTimer.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~StatusManager() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion


    }
}
