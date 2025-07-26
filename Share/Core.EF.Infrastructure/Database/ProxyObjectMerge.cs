using System.Diagnostics;
using System.Reflection;
using Core.Helper.Extend;

namespace Core.EF.Infrastructure.Database
{
    public  class ProxyObjectMerge
    {
        public Dictionary<string, object> dicMere = new Dictionary<string, object>();
        public  void MergeProp<T>(ref T obj1, T obj2) where T : class

        {

            if (obj2==null)
            {
                return;
            }
            if (!dicMere.ContainsKey(obj1.GetType() + "_" + obj1.GetPropValue("ID")))
            {
                Debug.WriteLine("MergeProp " + obj1.GetType().ToString() + " ID=" + obj1.GetPropValue("ID"));
                dicMere.Add(obj1.GetType() + "_" + obj1.GetPropValue("ID"), obj1);
                var pInfo = obj2.GetType().GetProperties();
                List<PropertyInfo> proName = new List<PropertyInfo>();
                foreach (var prop in pInfo)
                {
                    if (!prop.PropertyType.IsGenericList())
                    {
                        var p1 = obj1.GetType().GetProperty(prop.Name);
                        if (prop.PropertyType.ToString().ToUpper().StartsWith("System.".ToUpper()) || prop.PropertyType.IsEnum)
                        {
                            obj1.SetPropValue(prop.Name, obj2.GetPropValue(prop.Name));
                        }
                        else
                        {
                            var rsx = obj1.GetPropValue(prop.Name);
                            if (rsx != null)
                            {
                                if (dicMere.ContainsKey(p1.PropertyType + "_" + rsx.GetPropValue("ID")))
                                {
                                    var tt = dicMere[p1.PropertyType + "_" + rsx.GetPropValue("ID")];
                                    obj1.SetPropValue(prop.Name, tt);
                                    Debug.WriteLine("Irg Mapping "+ prop.Name + "> MergeProp " + p1.PropertyType + " ID=" + rsx.GetPropValue("ID"));

                                }
                                else
                                {

                                    dicMere.Add(p1.PropertyType + "_" + rsx.GetPropValue("ID"), rsx);
                                    Debug.WriteLine("MergeArrayProp > MergeProp " + p1.PropertyType + " ID=" + rsx.GetPropValue("ID"));
                                    this.GetType().GetMethod("MergeProp")
                                        ?.MakeGenericMethod(prop.PropertyType).Invoke(this, new object?[]
                                        {
                                        obj1.GetPropValue(prop.Name), obj2.GetPropValue(prop.Name)
                                        });

                                }
                            }

                        }

                    }
                    else
                    {
                        proName.Add(prop);
                    }

                }
                foreach (var prop in proName)
                {

                    if (!dicMere.ContainsKey(prop.PropertyType + "_" + prop.Name + "_" + obj1.GetPropValue("ID")))
                    {
                        dicMere.Add(prop.PropertyType + "_" + prop.Name + "_" + obj1.GetPropValue("ID"), obj1);
                        this.GetType().GetMethod("MergeArrayProp")
                            ?.MakeGenericMethod(prop.PropertyType.GetGenericArguments()[0]).Invoke(this, new object?[]
                            {
                            obj1.GetPropValue(prop.Name), obj2.GetPropValue(prop.Name),"ID"
                            });


                    }

                }
            }
            else
            {
                Debug.WriteLine("Irg Mapping > MergeProp " + obj1.GetType() + " ID=" + obj1.GetPropValue("ID"));
            }

        }
        public  void MergeArrayProp<T>(ref List<T>? obj1, List<T>? obj2,string name) where T : class
        {
            if (obj1==null)
            {
                obj1 = new List<T>();
                return ;
            }
            if (obj2 == null)
            {
                obj1.Clear();
                return ;
            }

           
            var lstR = (from item1 in obj1
                join item2 in obj2 on item1.GetPropValue(name) equals item2.GetPropValue(name)
                    into item1Item2 //Performing LINQ Group Join
                from item in item1Item2.DefaultIfEmpty()
                select new { Item1 = item1, Item2 = item }).Where(t=>t.Item2==null).Select(t=>t.Item1).ToList();
            foreach (var item in lstR)
            {
                obj1.Remove(item);

            }
            var lstU = (from item2 in obj2
                join item1 in obj1 on item2.GetPropValue(name) equals item1.GetPropValue(name)
                                   into item2Item1 //Performing LINQ Group Join
                               from item in item2Item1.DefaultIfEmpty()
                select new { Item2 = item2, Item1 = item }).ToList();

            foreach (var item in lstU.Where(t=>t.Item1==null))
            {
                if (item.Item1 == null)
                {
                    obj1.Add(item.Item2);
                }
               
            }
            foreach (var item in lstU.Where(t => t.Item1 != null))
            {
               
                var ref1 = obj1.First(t => t.GetPropValue(name).Equals(item.Item1.GetPropValue(name)));
                var ref2 = obj2.First(t => t.GetPropValue(name).Equals(item.Item2.GetPropValue(name)));
                if (!dicMere.ContainsKey(ref1.GetType() + "_" + ref1.GetPropValue("ID")))
                {
                    Debug.WriteLine("MergeArrayProp > MergeProp "+ref1.GetType().ToString() +" ID="+ ref1.GetPropValue("ID"));
                    MergeProp(ref ref1, ref2);
                }

             
              


            }
        }
    }
}
