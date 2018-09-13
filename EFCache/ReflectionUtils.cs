using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace EFCache
{
    public static class ReflectionUtils
    {
        private static readonly
            ConcurrentDictionary<string, ConcurrentDictionary<Type, Func<object, object>>> GettersCache
                = new ConcurrentDictionary<string, ConcurrentDictionary<Type, Func<object, object>>>();

        /// <summary>
        /// Gets a compiled property getter delegate for the underlying type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <param name="bindingFlags"></param>
        /// <returns></returns>
        public static Func<object, object> GetPropertyGetterDelegate(
            this Type type, string propertyName, BindingFlags bindingFlags)
        {
            var property = type.GetProperty(propertyName, bindingFlags);
            if (property == null)
                throw new InvalidOperationException($"Couldn't find the {propertyName} property.");

            var getMethod = property.GetGetMethod(nonPublic: true);
            if (getMethod == null)
                throw new InvalidOperationException($"Couldn't get the GetMethod of {type}");

            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var getterExpression = Expression.Convert(
                Expression.Call(Expression.Convert(instanceParam, type), getMethod), typeof(object));
            return Expression.Lambda<Func<object, object>>(getterExpression, instanceParam).Compile();
        }

        /// <summary>
        /// Gets a compiled property getter delegate for the underlying type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <param name="bindingFlags"></param>
        /// <returns></returns>
        public static Func<object, object> GetPropertyGetterDelegateFromCache(
            this Type type, string propertyName, BindingFlags bindingFlags)
        {
            Func<object, object> getter;
            if (GettersCache.TryGetValue(propertyName, out var getterDictionary))
            {
                if (getterDictionary.TryGetValue(type, out getter))
                {
                    return getter;
                }
            }

            getter = type.GetPropertyGetterDelegate(propertyName, bindingFlags);
            if (getter == null)
            {
                throw new NotSupportedException($"Failed to get {propertyName}-Getter.");
            }

            if (getterDictionary != null)
            {
                getterDictionary.TryAdd(type, getter);
            }
            else
            {
                GettersCache.TryAdd(propertyName,
                    new ConcurrentDictionary<Type, Func<object, object>>(
                        new Dictionary<Type, Func<object, object>>
                        {
                            { type, getter }
                        }));
            }

            return getter;
        }
    }
}