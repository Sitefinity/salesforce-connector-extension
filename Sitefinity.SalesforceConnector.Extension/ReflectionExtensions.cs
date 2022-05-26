using System;
using System.Linq;
using System.Reflection;

namespace Sitefinity.SalesforceConnector.Extension
{
    internal static class ReflectionExtensions
    {
        public static object GetPropertyValue(this object instance, string propertyName)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            var type = instance.GetType();
            PropertyInfo property = ReflectionExtensions.GetPropertyInfo(type, propertyName);
            if (property == null)
            {
                throw new ArgumentOutOfRangeException("propertyName", string.Format("Couldn't find property {0} in type {1}", propertyName, type.FullName));
            }

            return property.GetValue(instance, null);
        }

        public static void SetPropertyValue(this object instance, string propertyName, object value)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            var type = instance.GetType();
            PropertyInfo property = ReflectionExtensions.GetPropertyInfo(type, propertyName);
            if (property == null)
            {
                throw new ArgumentOutOfRangeException("propertyName", string.Format("Couldn't find property {0} in type {1}", propertyName, type.FullName));
            }

            property.SetValue(instance, value, null);
        }

        public static T ExecuteMethod<T>(this object target, string methodName, object[] arguments)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            var type = target.GetType();
            var method = ReflectionExtensions.GetMethodInfo(type, methodName);
            if (method == null)
            {
                throw new ArgumentOutOfRangeException("methodName", string.Format("Couldn't find method {0} in type {1}", methodName, type.FullName));
            }

            return (T)method.Invoke(target, arguments);
        }

        public static T CreateInstance<T>(this Type type, params object[] arguments)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            var constructor = ReflectionExtensions.GetConstructorInfo(type, arguments.Select(a => a.GetType()).ToArray());
            if (constructor == null)
            {
                throw new ArgumentOutOfRangeException("arguments", string.Format("Couldn't find constructor matching specified arguments in type {0}", type.FullName));
            }

            return (T)constructor.Invoke(arguments);
        }

        private static MethodInfo GetMethodInfo(Type type, string methodName)
        {
            MethodInfo result = null;
            do
            {
                result = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            while (result == null && type != null);

            return result;
        }

        private static PropertyInfo GetPropertyInfo(Type type, string propertyName)
        {
            PropertyInfo result = null;
            do
            {
                result = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }
            while (result == null && type != null);

            return result;
        }

        private static ConstructorInfo GetConstructorInfo(Type type, Type[] arguments)
        {
            ConstructorInfo result = null;
            do
            {
                result = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, arguments, null);
                type = type.BaseType;
            }
            while (result == null && type != null);

            return result;
        }
    }
}
