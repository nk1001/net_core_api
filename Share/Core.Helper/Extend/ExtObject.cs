using System.Collections;
using System.Collections.Specialized;
using System.Reflection;

namespace Core.Helper.Extend
{
    public static class ExtObject
    {
        public static string ToStringIgnoeNull(this object obj)
        {
            if (obj == null)
                return string.Empty;
            return obj.ToString();
        }
        public static string JoinStrings(this object[] obj, string join)
        {
            if (obj == null)
            {
                return string.Empty;
            }
            return string.Join(join, obj);
        }
        public static NameValueCollection ToNameValueCollection(this object obj)
        {
            if (obj is NameValueCollection)
            {
                return (NameValueCollection)obj;
            }
            NameValueCollection nameValueCollection = new NameValueCollection();
            foreach (var item in obj.GetType().GetProperties())
            {
                if (obj.GetPropValue(item.Name)!=null)
                {
                    nameValueCollection.Add(item.Name, obj.GetPropValue(item.Name).ToString());
                }              
            
            }
            return nameValueCollection;
           
        }
        public static void SetPropValue(this object obj, String propName, object value)
        {
            var _obj = (object)null;
            string[] nameParts = propName.Split('.');
            if (nameParts.Length == 1)
            {
                string index = "-1";
                if (propName.IndexOf("[") > -1 && propName.IndexOf("]") > -1)
                {
                    index = propName.Substring(propName.IndexOf("[") + 1, propName.IndexOf("]") - (propName.IndexOf("[") + 1));
                    propName = propName.Substring(0, propName.IndexOf("["));
                }
                if (index == "-1")
                {
                    obj.GetType().GetProperty(propName).SetValue(obj, value);
                    return;

                }
                ((IList)obj.GetType().GetProperty(propName).GetValue(obj, null))[int.Parse(index)] = value;
                return;
            }
            foreach (String part in nameParts)
            {
                if (_obj == null)
                {
                    _obj = obj;
                }
                var _item = part;
                if (_obj == null) { return; }

                Type type = _obj.GetType();
                string index = "-1";
                if (_item.IndexOf("[") > -1 && _item.IndexOf("]") > -1)
                {
                    index = _item.Substring(_item.IndexOf("[") + 1, _item.IndexOf("]") - (_item.IndexOf("[") + 1));
                    _item = _item.Substring(0, _item.IndexOf("["));
                }
                PropertyInfo info = type.GetProperty(_item);
                if (info == null) { return; }
                if (index != "-1")
                {
                    ((IList)info.GetValue(_obj, null))[int.Parse(index)] = value;
                }
                else
                {
                    info.SetValue(_obj, value);

                }
            }
        }
        public static object? GetPropValue(this object? obj, String propName)
        {
            var _obj = (object)null;
            try
            {
                string[] nameParts = propName.Split('.');
                if (nameParts.Length == 1)
                {
                    string index = "-1";
                    if (propName.IndexOf("[") > -1 && propName.IndexOf("]") > -1)
                    {
                        index = propName.Substring(propName.IndexOf("[") + 1, propName.IndexOf("]") - (propName.IndexOf("[") + 1));
                        propName = propName.Substring(0, propName.IndexOf("["));
                    }
                    if (index == "-1")
                    {
                        return obj.GetType().GetProperty(propName).GetValue(obj, null);
                    }
                    return ((IList)obj.GetType().GetProperty(propName).GetValue(obj, null))[int.Parse(index)];
                }

                foreach (String part in nameParts)
                {
                    if (_obj == null)
                    {
                        _obj = obj;
                    }
                    var _item = part;
                    if (_obj == null) { return null; }

                    Type type = _obj.GetType();
                    string index = "-1";
                    if (_item.IndexOf("[") > -1 && _item.IndexOf("]") > -1)
                    {
                        index = _item.Substring(_item.IndexOf("[") + 1, _item.IndexOf("]") - (_item.IndexOf("[") + 1));
                        _item = _item.Substring(0, _item.IndexOf("["));
                    }
                    PropertyInfo info = type.GetProperty(_item);
                    if (info == null) { return null; }
                    _obj = info.GetValue(_obj, null);
                    if (index != "-1")
                    {
                        _obj = ((IList)_obj)[int.Parse(index)];
                    }
                }
            }
            catch (Exception e)
            {
                // ignored
            }

            return _obj;
        }

    }
}
