using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Caliburn.Micro;
using SMAStudiovNext.Core;

namespace SMAStudiovNext.Utils
{
    public class ResourceContainer : PropertyChangedBase, IDisposable
    {
        private ObservableCollection<ResourceContainer> _items;
        private object _viewModel = null;
        private object _tag = null;
        private string _title = string.Empty;
        private bool _isExpanded = false;
        private string _icon = string.Empty;

        #region Constructors
        public ResourceContainer()
        {
            _items = new ObservableCollection<ResourceContainer>();
        }

        public ResourceContainer(string title)
        {
            Title = title;
            _items = new ObservableCollection<ResourceContainer>();
        }

        public ResourceContainer(string title, object tag)
        {
            Title = title;
            Tag = tag;
            _items = new ObservableCollection<ResourceContainer>();
        }

        public ResourceContainer(string title, object tag, string icon)
        {
            Title = title;
            Tag = tag;
            Icon = icon;
            _items = new ObservableCollection<ResourceContainer>();
        }

        public ResourceContainer(string title, object tag, string icon, IBackendContext context)
        {
            Title = title;
            Tag = tag;
            Icon = icon;
            Context = context;
            _items = new ObservableCollection<ResourceContainer>();
        }
        #endregion

        private void OnModelChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyOfPropertyChange(() => Title);
        }

        #region Properties
        public string Title
        {
            get
            {
                /*if (Tag != null && Tag is SMA.Runbook)
                {
                    return ((SMA.Runbook)Tag).RunbookName;
                }*/

                return _title;
            }
            set { _title = value; }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value.Equals(_isExpanded))
                    return;

                _isExpanded = value;
                NotifyOfPropertyChange(() => IsExpanded);
            }
        }

        public string Icon
        {
            get
            {
                return _icon;
            }
            set { _icon = value; NotifyOfPropertyChange(() => Icon); }
        }

        public object Tag
        {
            get { return _tag; }
            set
            {
                _tag = value;
                NotifyOfPropertyChange(() => Title);
            }
        }

        public ObservableCollection<ResourceContainer> Items
        {
            get { return _items; }
            set { _items = value; }
        }

        public IBackendContext Context { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is ResourceContainer))
                return false;

            return (Tag.Equals(((ResourceContainer)obj).Tag));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_viewModel is PropertyChangedBase)
                        ((PropertyChangedBase)_viewModel).PropertyChanged -= OnModelChanged;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~EnvironmentViewItem() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
