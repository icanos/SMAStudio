using SMAStudiovNext.Core;
using SMAStudiovNext.Services;
using System;
using System.Reflection;

namespace SMAStudiovNext.Models
{
    public class ModelProxyBase
    {
        protected object instance;
        protected Type instanceType;
        protected object viewModel;
        protected readonly IBackendService _ownerService;

        /// <summary>
        /// Creates or returns the cached view model of the object passed to this function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public T GetViewModel<T>()
        {
            if (viewModel != null)
                return (T)viewModel;

            var type = typeof(T);
            T obj = default(T);

            obj = (T)Activator.CreateInstance(type, this);
            ((IViewModel)obj).Owner = Context.Service;

            viewModel = obj;

            return obj;
        }

        protected PropertyInfo GetProperty(string name)
        {
            var property = instanceType.GetProperty(name);

            if (property == null)
                throw new InvalidOperationException(instanceType + " doesn't contain the property '" + name + "'.");

            return property;
        }

        public Type GetSubType()
        {
            return instance.GetType();
        }

        internal object ViewModel
        {
            set { viewModel = value; }
        }

        public object Model
        {
            get { return instance; }
        }

        public IBackendContext Context
        {
            get;
            set;
        }
    }
}
