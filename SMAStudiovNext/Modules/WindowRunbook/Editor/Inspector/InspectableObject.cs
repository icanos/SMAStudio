using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace SMAStudiovNext.Modules.WindowRunbook.Editor.Inspector
{
    public abstract class InspectableObjectBase : ICustomTypeDescriptor
    {
        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return null;
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        public PropertyDescriptorCollection GetProperties()
        {
            return ((ICustomTypeDescriptor)this).GetProperties(new Attribute[0]);
        }

        public virtual PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return null;
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return pd;
        }
    }

    // ReSharper disable once InconsistentNaming
    public class InspectablePSObject : InspectableObjectBase
    {
        private readonly PSObject _obj;

        public InspectablePSObject(PSObject obj)
        {
            _obj = obj;
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            var objectProperties = _obj.Properties;
            var properties = new ArrayList();

            foreach (var prop in objectProperties)
            {
                if (prop.Value == null)
                    properties.Add(new InspectablePropertyDescriptor(prop.Name, "(null)"));
                else
                    properties.Add(new InspectablePropertyDescriptor(prop.Name, prop.Value));
            }

            PropertyDescriptor[] props =
                (PropertyDescriptor[])properties.ToArray(typeof(PropertyDescriptor));

            return new PropertyDescriptorCollection(props);
        }

        public override string ToString()
        {
            return _obj.ToString();
        }
    }

    public class InspectableDictionaryObject : InspectableObjectBase
    {
        private readonly IDictionary<string, object> _dictionary;
         
        public InspectableDictionaryObject(IDictionary<string, object> dictionary)
        {
            _dictionary = dictionary;
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            ArrayList properties = new ArrayList();
            foreach (KeyValuePair<string, object> e in _dictionary)
            {
                properties.Add(new InspectablePropertyDescriptor(e.Key, e.Value));
            }

            PropertyDescriptor[] props =
                (PropertyDescriptor[])properties.ToArray(typeof(PropertyDescriptor));

            return new PropertyDescriptorCollection(props);
        }
    }

    public class InspectableCollectionObject<T> : InspectableObjectBase, IList<T>
    {
        private readonly IList<T> _collection;

        public InspectableCollectionObject(IList<T> collection)
        {
            _collection = collection;
            //_collection = new List<collection;
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            var properties = new ArrayList();

            if (_collection.Count > 1)
            {
                // This is probably a list
                var idx = 0;
                foreach (var e in _collection)
                {
                    properties.Add(new InspectablePropertyDescriptor("[" + idx + "]", e));
                    idx++;
                }
            }
            else if (_collection.Count > 0)
            {
                // Single object
                var obj = _collection[0];

                if (obj is Hashtable)
                {
                    foreach (var e in (obj as Hashtable).Keys)
                    {
                        properties.Add(new InspectablePropertyDescriptor(e.ToString(), (obj as Hashtable)[e]));
                    }
                }
            }
            /*if (_collection is Hashtable)
            {
                foreach (var e in (_collection as Hashtable).Keys)
                {
                    properties.Add(new InspectablePropertyDescriptor(e.ToString(), (_collection as Hashtable)[e]));
                }
            }
            else if ((_collection is IList))
            {
                
            }*/

            PropertyDescriptor[] props =
                (PropertyDescriptor[])properties.ToArray(typeof(PropertyDescriptor));

            return new PropertyDescriptorCollection(props);
        }

       /* public T this[int index]
        {
            get { return _collection[index]; }
        }*/

        public IEnumerator<T> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        public override string ToString()
        {
            return "(collection)";
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _collection).GetEnumerator();
        }

        public void Add(T item)
        {
            _collection.Add(item);
        }

        public void Clear()
        {
            _collection.Clear();
        }

        public bool Contains(T item)
        {
            return _collection.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _collection.Remove(item);
        }

        public int Count
        {
            get { return _collection.Count; }
        }

        public bool IsReadOnly
        {
            get { return _collection.IsReadOnly; }
        }

        public int IndexOf(T item)
        {
            return _collection.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _collection.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _collection.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return _collection[index]; }
            set { _collection[index] = value; }
        }
    }

    class InspectablePropertyDescriptor : PropertyDescriptor
    {
        private string _key;
        private object _value;

        internal InspectablePropertyDescriptor(string key, object value) : base (key, null)
        {
            _key = key;
            _value = value;
        }

        #region Constructors (not used)
        public InspectablePropertyDescriptor(string name, Attribute[] attrs) : base(name, attrs)
        {
        }

        public InspectablePropertyDescriptor(MemberDescriptor descr) : base(descr)
        {
        }

        public InspectablePropertyDescriptor(MemberDescriptor descr, Attribute[] attrs) : base(descr, attrs)
        {
        }
        #endregion

        protected override AttributeCollection CreateAttributeCollection()
        {
            var attrib = new ExpandableObjectAttribute();
            var collection = new AttributeCollection(attrib);

            return collection;
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override object GetValue(object component)
        {
            var value = _value as Hashtable;
            if (value != null)
                return new InspectableCollectionObject<Hashtable>(new List<Hashtable> {value});

            if (_value is string)
            {
                // Make sure to return the string in UTF8
                var data = Encoding.Default.GetBytes(_value.ToString());
                return Encoding.UTF8.GetString(data);
            }
            
            if (_value.GetType().ToString().Equals("System.Object[]"))
                return _value;

            if (
                _value.GetType()
                    .ToString()
                    .Equals("System.Management.Automation.PSDataCollection`1[System.Management.Automation.PSObject]"))
            {
                var value3 = _value as PSDataCollection<PSObject>;
                if (value3 != null)
                {
                    var list = value3.Select(item => new InspectablePSObject(item)).ToList();
                    return new InspectableCollectionObject<InspectablePSObject>(list);
                }
            }

            var value2 = _value as IList<object>;
            if (value2 != null)
            {
                return new InspectableCollectionObject<object>(value2);
            }

            return _value;
        }

        public override void ResetValue(object component)
        {
        }

        public override void SetValue(object component, object value)
        {
            _value = value;
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        public object Value
        {
            get { return "VALUE!!"; }
        }

        public override Type ComponentType => null;
        public override bool IsReadOnly => false;
        public override Type PropertyType => _value.GetType();
    }
}
