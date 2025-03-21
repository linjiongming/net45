using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using NLog.Common;

namespace NLog.Extensions.Logging
{
    using ExtractorDictionary = ConcurrentDictionary<Type, KeyValuePair<Func<object, object>, Func<object, object>>>;

    /// <summary>
    /// Converts Microsoft Extension Logging BeginScope into NLog ScopeContext
    /// </summary>
    internal class NLogBeginScopeParser
    {
        private readonly NLogProviderOptions _options;

        private readonly ExtractorDictionary _scopeStateExtractors =
            new ExtractorDictionary();

        public NLogBeginScopeParser(NLogProviderOptions options)
        {
            _options = options ?? NLogProviderOptions.Default;
        }

        public IDisposable ParseBeginScope<T>(T state)
        {
            if (_options.CaptureMessageProperties)
            {
                if (state is IReadOnlyList<KeyValuePair<string, object>> scopePropertyList)
                {
                    if (scopePropertyList is IList)
                        return ScopeContext.PushNestedStateProperties(null, scopePropertyList);  // Probably List/Array without nested state

                    object scopeObject = scopePropertyList;
                    scopePropertyList = ParseScopeProperties(scopePropertyList);
                    return ScopeContext.PushNestedStateProperties(scopeObject, scopePropertyList);
                }
                else if (state is IReadOnlyCollection<KeyValuePair<string, object>> scopeProperties)
                {
                    if (scopeProperties is IDictionary)
                        return ScopeContext.PushNestedStateProperties(null, scopeProperties);    // Probably Dictionary without nested state
                    else
                        return ScopeContext.PushNestedStateProperties(scopeProperties, scopeProperties);
                }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER || NET471_OR_GREATER
                else if (state is System.Runtime.CompilerServices.ITuple tuple && tuple.Length == 2 && tuple[0] is string)
                {
                    return ScopeContext.PushProperty(tuple[0].ToString(), tuple[1]);
                }
#endif

                if (!(state is string))
                {
                    if (state is IEnumerable scopePropertyCollection)
                    {
                        return CaptureScopeProperties(scopePropertyCollection, _scopeStateExtractors);
                    }

                    return CaptureScopeProperty(state, _scopeStateExtractors);
                }
            }

            return ScopeContext.PushNestedState(state);
        }

        private IReadOnlyList<KeyValuePair<string, object>> ParseScopeProperties(IReadOnlyList<KeyValuePair<string, object>> scopePropertyList)
        {
            int scopePropertyCount = scopePropertyList.Count;
            if (scopePropertyCount == 0)
                return scopePropertyList;

            if (!NLogLogger.OriginalFormatPropertyName.Equals(scopePropertyList[scopePropertyCount - 1].Key))
                return IncludeActivityIdsProperties(scopePropertyList);
            else if (scopePropertyCount == 1)
                return new KeyValuePair<string, object>[] {};
            else
                scopePropertyCount -= 1;    // Handle BeginScope("Hello {World}", "Earth")

            KeyValuePair<string, object> firstProperty = scopePropertyList[0];
            if (scopePropertyCount == 1 && !string.IsNullOrEmpty(firstProperty.Key))
            {
                return new[] { firstProperty };
            }
            else
            {
                List<KeyValuePair<string, object>> propertyList = new List<KeyValuePair<string, object>>(scopePropertyCount);
                for (int i = 0; i < scopePropertyCount; ++i)
                {
                    KeyValuePair<string, object> property = scopePropertyList[i];
                    if (string.IsNullOrEmpty(property.Key))
                    {
                        continue;
                    }
                    propertyList.Add(property);
                }
                return propertyList;
            }
        }
        
        private static IReadOnlyList<KeyValuePair<string, object>> IncludeActivityIdsProperties(IReadOnlyList<KeyValuePair<string, object>> scopePropertyList)
        {
            return scopePropertyList;   // Not supported
        }

        public static IDisposable CaptureScopeProperties(IEnumerable scopePropertyCollection, ExtractorDictionary stateExtractor)
        {
            List<KeyValuePair<string, object>> propertyList = null;

            KeyValuePair<Func<object, object>, Func<object, object>> keyValueExtractor = default(KeyValuePair<Func<object, object>, Func<object, object>>);
            foreach (object property in scopePropertyCollection)
            {
                if (property is null)
                {
                    break;
                }

                if (keyValueExtractor.Key is null && !TryLookupExtractor(stateExtractor, property.GetType(), out keyValueExtractor))
                {
                    break;
                }

                KeyValuePair<string, object>? propertyValue = TryParseKeyValueProperty(keyValueExtractor, property);
                if (!propertyValue.HasValue)
                {
                    continue;
                }

                propertyList = propertyList ?? new List<KeyValuePair<string, object>>((scopePropertyCollection as ICollection)?.Count ?? 0);
                propertyList.Add(propertyValue.Value);
            }

            if (scopePropertyCollection is IList || scopePropertyCollection is IDictionary)
                return ScopeContext.PushNestedStateProperties(null, propertyList);   // Probably List/Array/Dictionary without nested state
            else
                return ScopeContext.PushNestedStateProperties(scopePropertyCollection, propertyList);
        }

        public static IDisposable CaptureScopeProperty<TState>(TState scopeProperty, ExtractorDictionary stateExtractor)
        {
            if (!TryLookupExtractor(stateExtractor, scopeProperty.GetType(), out KeyValuePair<Func<object, object>, Func<object, object>> keyValueExtractor))
            {
                return ScopeContext.PushNestedState(scopeProperty);
            }

            object scopePropertyValue = scopeProperty;
            KeyValuePair<string, object>? propertyValue = TryParseKeyValueProperty(keyValueExtractor, scopePropertyValue);
            if (propertyValue.HasValue)
            {
                return ScopeContext.PushNestedStateProperties(scopePropertyValue, new[] { new KeyValuePair<string, object>(propertyValue.Value.Key, propertyValue.Value.Value) });
            }

            return ScopeContext.PushNestedState(scopeProperty);
        }

        private static KeyValuePair<string, object>? TryParseKeyValueProperty(KeyValuePair<Func<object, object>, Func<object, object>> keyValueExtractor, object property)
        {
            string propertyName = null;

            try
            {
                object propertyKey = keyValueExtractor.Key.Invoke(property);
                propertyName = propertyKey?.ToString() ?? string.Empty;
                object propertyValue = keyValueExtractor.Value.Invoke(property);
                return new KeyValuePair<string, object>(propertyName, propertyValue);
            }
            catch (Exception ex)
            {
                InternalLogger.Debug(ex, "Exception in BeginScope add property {0}", propertyName);
                return null;
            }
        }

        private static bool TryLookupExtractor(ExtractorDictionary stateExtractor, Type propertyType,
            out KeyValuePair<Func<object, object>, Func<object, object>> keyValueExtractor)
        {
            if (!stateExtractor.TryGetValue(propertyType, out keyValueExtractor))
            {
                try
                {
                    return TryBuildExtractor(propertyType, out keyValueExtractor);
                }
                catch (Exception ex)
                {
                    InternalLogger.Debug(ex, "Exception in BeginScope create property extractor");
                }
                finally
                {
                    stateExtractor[propertyType] = keyValueExtractor;
                }
            }

            return keyValueExtractor.Key != null;
        }

        private static bool TryBuildExtractor(Type propertyType, out KeyValuePair<Func<object, object>, Func<object, object>> keyValueExtractor)
        {
            keyValueExtractor = default(KeyValuePair<Func<object, object>, Func<object, object>>);

            TypeInfo itemType = propertyType.GetTypeInfo();
            if (!itemType.IsGenericType)
                return false;

            if (itemType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                PropertyInfo keyPropertyInfo = typeof(KeyValuePair<,>).MakeGenericType(itemType.GenericTypeArguments).GetTypeInfo().GetDeclaredProperty("Key");
                PropertyInfo valuePropertyInfo = typeof(KeyValuePair<,>).MakeGenericType(itemType.GenericTypeArguments).GetTypeInfo().GetDeclaredProperty("Value");
                if (valuePropertyInfo is null || keyPropertyInfo is null)
                {
                    return false;
                }

                ParameterExpression keyValuePairObjParam = Expression.Parameter(typeof(object), "KeyValuePair");
                UnaryExpression keyValuePairTypeParam = Expression.Convert(keyValuePairObjParam, propertyType);
                MemberExpression propertyKeyAccess = Expression.Property(keyValuePairTypeParam, keyPropertyInfo);
                MemberExpression propertyValueAccess = Expression.Property(keyValuePairTypeParam, valuePropertyInfo);
                return BuildKeyValueExtractor(keyValuePairObjParam, propertyKeyAccess, propertyValueAccess, out keyValueExtractor);
            }

            return false;
        }

        private static bool BuildKeyValueExtractor(ParameterExpression keyValuePairObjParam, MemberExpression propertyKeyAccess, MemberExpression propertyValueAccess, out KeyValuePair<Func<object, object>, Func<object, object>> keyValueExtractor)
        {
            UnaryExpression propertyKeyAccessObj = Expression.Convert(propertyKeyAccess, typeof(object));
            Func<object, object> propertyKeyLambda = Expression.Lambda<Func<object, object>>(propertyKeyAccessObj, keyValuePairObjParam).Compile();

            UnaryExpression propertyValueAccessObj = Expression.Convert(propertyValueAccess, typeof(object));
            Func<object, object> propertyValueLambda = Expression.Lambda<Func<object, object>>(propertyValueAccessObj, keyValuePairObjParam).Compile();

            keyValueExtractor = new KeyValuePair<Func<object, object>, Func<object, object>>(propertyKeyLambda, propertyValueLambda);
            return true;
        }
    }
}