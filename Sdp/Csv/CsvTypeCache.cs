using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using Sdp.Attributes;
using Sdp.Resources;

namespace Sdp.Csv;

internal static class CsvTypeCache
{
    private static readonly ConcurrentDictionary<Type, TypeMappingInfo> TypeCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> ImmutableArrayCreateCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> FrozenSetHelperCache = new();
    private static readonly ConcurrentDictionary<(Type, Type), MethodInfo> FrozenDictionaryHelperCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo> KeyPropertyCache = new();

    public static TypeMappingInfo GetTypeInfo(Type type)
    {
        return TypeCache.GetOrAdd(type, static t =>
        {
            var ctor = t.GetConstructors().First();
            var parameters = ctor.GetParameters();
            var paramInfos = new ParameterMappingInfo[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                paramInfos[i] = CreateParameterMappingInfo(parameters[i]);
            }

            return new TypeMappingInfo(ctor, paramInfos);
        });
    }

    public static MethodInfo GetImmutableArrayCreateMethod(Type elementType)
    {
        return ImmutableArrayCreateCache.GetOrAdd(elementType, static et =>
            typeof(ImmutableArray)
                .GetMethods()
                .Where(x => x.Name == nameof(ImmutableArray.Create))
                .Where(x => x.GetParameters().Length is 1)
                .First(x => x.GetParameters()[0].ParameterType.IsArray)
                .MakeGenericMethod(et));
    }

    public static MethodInfo GetFrozenSetHelperMethod(Type elementType, string helperMethodName)
    {
        return FrozenSetHelperCache.GetOrAdd(elementType, et =>
            typeof(CsvRecordMapper)
                .GetMethod(helperMethodName, BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(et));
    }

    public static MethodInfo GetFrozenDictionaryHelperMethod(Type keyType, Type valueType, string helperMethodName)
    {
        return FrozenDictionaryHelperCache.GetOrAdd((keyType, valueType), types =>
            typeof(CsvRecordMapper)
                .GetMethod(helperMethodName, BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(types.Item1, types.Item2));
    }

    public static PropertyInfo GetKeyProperty(Type valueType)
    {
        return KeyPropertyCache.GetOrAdd(valueType, static vt =>
        {
            var ctor = vt.GetConstructors().First();
            var keyParam = ctor.GetParameters()
                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() is not null);

            if (keyParam is null)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.KeyAttributeRequired,
                    vt.Name));
            }

            var keyPropertyName = keyParam.Name!;
            var property = vt.GetProperty(
                keyPropertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property is null)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    Messages.Composite.KeyPropertyNotFound,
                    keyPropertyName,
                    vt.Name));
            }

            return property;
        });
    }

    private static ParameterMappingInfo CreateParameterMappingInfo(ParameterInfo param)
    {
        var columnNameAttr = param.GetCustomAttribute<ColumnNameAttribute>();
        var lengthAttr = param.GetCustomAttribute<LengthAttribute>();
        var nullStringAttr = param.GetCustomAttribute<NullStringAttribute>();
        var singleColumnAttr = param.GetCustomAttribute<SingleColumnCollectionAttribute>();

        var paramType = param.ParameterType;
        var collectionType = CollectionKind.None;
        Type? elementType = null;
        Type? keyType = null;
        string? singleColumnSeparator = null;

        if (singleColumnAttr is not null && paramType.IsGenericType)
        {
            var genericTypeDef = paramType.GetGenericTypeDefinition();
            if (genericTypeDef == typeof(ImmutableArray<>))
            {
                collectionType = CollectionKind.SingleColumnImmutableArray;
                elementType = paramType.GetGenericArguments()[0];
                singleColumnSeparator = singleColumnAttr.Separator;
            }
        }
        else if (lengthAttr is not null && paramType.IsGenericType)
        {
            var genericTypeDef = paramType.GetGenericTypeDefinition();
            if (genericTypeDef == typeof(ImmutableArray<>))
            {
                collectionType = CollectionKind.ImmutableArray;
                elementType = paramType.GetGenericArguments()[0];
            }
            else if (genericTypeDef == typeof(FrozenSet<>))
            {
                collectionType = CollectionKind.FrozenSet;
                elementType = paramType.GetGenericArguments()[0];
            }
            else if (genericTypeDef == typeof(FrozenDictionary<,>))
            {
                collectionType = CollectionKind.FrozenDictionary;
                var genericArgs = paramType.GetGenericArguments();
                keyType = genericArgs[0];
                elementType = genericArgs[1];
            }
        }

        return new ParameterMappingInfo(
            columnNameAttr?.Name ?? param.Name ?? string.Empty,
            paramType,
            lengthAttr?.Length,
            nullStringAttr?.NullString,
            collectionType,
            elementType,
            keyType,
            singleColumnSeparator);
    }
}

internal sealed record TypeMappingInfo(ConstructorInfo Constructor, ParameterMappingInfo[] Parameters);

internal sealed record ParameterMappingInfo(
    string ColumnName,
    Type ParameterType,
    int? Length,
    string? NullString,
    CollectionKind CollectionKind,
    Type? ElementType,
    Type? KeyType,
    string? SingleColumnSeparator);

internal enum CollectionKind
{
    None,
    ImmutableArray,
    FrozenSet,
    FrozenDictionary,
    SingleColumnImmutableArray,
}
