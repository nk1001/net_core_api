using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace Core.Helper.Extend
{
  public static class ExtType
  {

      public static bool IsEnumerableType(this Type type)
      {
          return (type.Name != nameof(String)
                  && type.GetInterface(nameof(IEnumerable)) != null);
        }
      public static bool IsCollectionType(this Type type)
      {
          return (type.GetInterface(nameof(ICollection)) != null);
      }
      public static bool DoesTypeSupportInterface(this Type type, Type inter)
      {
          if (inter.IsAssignableFrom(type))
              return true;
          if (type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == inter))
              return true;
          return false;
      }
        [DebuggerStepThrough]
    public static bool HasDefaultConstructor(this Type instance)
    {
      return ((IEnumerable<ConstructorInfo>) instance.GetConstructors(BindingFlags.Instance | BindingFlags.Public)).Any<ConstructorInfo>((Func<ConstructorInfo, bool>) (ctor => ctor.GetParameters().Length == 0));
    }

    [DebuggerStepThrough]
    public static IEnumerable<Type> PublicTypes(this Assembly instance)
    {
      IEnumerable<Type> types = (IEnumerable<Type>) null;
      if (instance != (Assembly) null)
      {
        try
        {
          types = ((IEnumerable<Type>) instance.GetTypes()).Where<Type>((Func<Type, bool>) (type =>
          {
            if (type != (Type) null && type.IsPublic)
              return type.IsVisible;
            return false;
          }));
        }
        catch (ReflectionTypeLoadException ex)
        {
          types = (IEnumerable<Type>) ex.Types;
        }
      }
      return types ?? Enumerable.Empty<Type>();
    }

    [DebuggerStepThrough]
    public static IEnumerable<Type> PublicTypes(this IEnumerable<Assembly> instance)
    {
      if (instance != null)
        return instance.SelectMany<Assembly, Type>((Func<Assembly, IEnumerable<Type>>) (assembly => assembly.PublicTypes()));
      return Enumerable.Empty<Type>();
    }

    [DebuggerStepThrough]
    public static IEnumerable<Type> ConcreteTypes(this Assembly instance)
    {
      if (!(instance == (Assembly) null))
        return instance.PublicTypes().Where<Type>((Func<Type, bool>) (type =>
        {
          if (type != (Type) null && type.IsClass && (!type.IsAbstract && !type.IsInterface))
            return !type.IsGenericType;
          return false;
        }));
      return Enumerable.Empty<Type>();
    }

    [DebuggerStepThrough]
    public static IEnumerable<Type> ConcreteTypes(this IEnumerable<Assembly> instance)
    {
      if (instance != null)
        return instance.SelectMany<Assembly, Type>((Func<Assembly, IEnumerable<Type>>) (assembly => assembly.ConcreteTypes()));
      return Enumerable.Empty<Type>();
    }
  }
}
