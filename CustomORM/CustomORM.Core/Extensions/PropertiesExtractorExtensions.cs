using CustomORM.Core.Abstractions;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace CustomORM.Core.Extensions;

public static class PropertiesExtractorExtensions
{
    private static readonly char[] CommaSeparator = [','];

    /// <summary>
    /// Create mapping profile
    /// </summary>
    /// <param name="className"></param>
    /// <returns></returns>
    internal static Dictionary<string, string> GetMappingNamesColumnsProperties(this Type className)
    {
        var namespaceOfEntite = className.Namespace;
        var mapping = new Dictionary<string, string>();

        foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(className))
        {
            if (!prop.PropertyType.FullName!.Contains(namespaceOfEntite!))
            {
                var attributeColumn = prop.Attributes[typeof(ColumnAttribute)] as ColumnAttribute;

                mapping.TryAdd(prop.Name, attributeColumn!.Name ?? prop.Name);
            }
        }

        return mapping;
    }

    /// <summary>
    /// Get column name associate with
    /// </summary>
    /// <param name="className"></param>
    /// <returns></returns>
    internal static string GetNamesColumns(this Type className)
    {
        var namespaceOfEntite = className.Namespace;
        List<string> columnsNames = [];

        foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(className))
        {
            if (!prop.PropertyType.FullName!.Contains(namespaceOfEntite!))
            {
                if (prop.Attributes[typeof(ColumnAttribute)] is ColumnAttribute attributeColumn)
                    columnsNames.Add($"@{attributeColumn.Name}");
                else
                    columnsNames.Add($"@{prop.Name}");
            }
        }

        return string.Join(",", columnsNames);
    }

    /// <summary>
    /// Convert to dynamic params (dapper)
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    internal static DynamicParameters ConvertToParamsRequest(this object obj, Type? type = null)
    {
        var dbArgs = new DynamicParameters();

        var mapping = type == null ?
            obj.GetType().GetMappingNamesColumnsProperties() :
            type.GetMappingNamesColumnsProperties();

        var columns = type == null ?
            obj.GetType().GetNamesColumns().Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries) :
            type.GetNamesColumns().Split(CommaSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var column in columns)
        {
            var columnName = column[1..];
            var propertyTarget = mapping.FirstOrDefault(x => x.Value == columnName).Key;

            dbArgs.Add($"{columnName}", obj.GetValue(propertyTarget));
        }

        return dbArgs;
    }

    internal static object GetValue(this object obj, string propertyName)
    {
        dynamic dyn = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(obj))!;

        return ((JValue)dyn[propertyName]).Value!;
    }

    internal static object? GetObjectProperty(this object obj, string propertyName)
    {
        var name = FindPropertyByAttribute(obj, propertyName);

        if (name != null)
        {
            PropertyInfo propertyInfo = obj.GetType().GetProperty(name)!;

            return propertyInfo?.GetValue(obj, null);
        }

        return null;
    }

    internal static PropertyDescriptorCollection GetPropertiesDescription(string fullName)
    {
        var pMethode1 = TypeDescriptor.GetProperties(Type.GetType(fullName)!);

        if (pMethode1.Count > 0)
            return pMethode1;

        var types = Assembly.GetEntryAssembly()?.GetTypes();
        var filteredType = types?.Where(t => t.FullName == fullName).First();

        var pMethode2 = TypeDescriptor.GetProperties(filteredType!);

        return pMethode2;
    }

    private static PropertyDescriptorCollection GetPropertiesDescription(this object obj)
    {
        var pMethode1 = TypeDescriptor.GetProperties(Type.GetType(obj.GetType().FullName!)!);

        if (pMethode1.Count > 0)
            return pMethode1;

        var types = Assembly.GetEntryAssembly()?.GetTypes();
        var filteredType = types?.Where(t => t.FullName == obj.GetType().FullName).First();

        var pMethode2 = TypeDescriptor.GetProperties(filteredType!);

        return pMethode2;
    }

    private static string? FindPropertyByAttribute(object obj, string propertyName)
    {
        foreach (PropertyDescriptor prop in obj.GetPropertiesDescription())
        {
            if (!prop.PropertyType.FullName!.Contains(obj.GetType().Namespace!))
            {
                var attributeColumn = prop.Attributes[typeof(ColumnAttribute)] as ColumnAttribute;

                if ((propertyName == prop.Name) || propertyName == attributeColumn?.Name)
                    return prop.Name;
            }
        }

        Log.Information($"Property {propertyName} not found in {obj.GetType().FullName}");

        return null;
    }

    public static string FindTableTargetInformation(this Type obj, TableSqlInfos tableSqlInfos)
    {
        var attr = obj.GetCustomAttribute(typeof(TableAttribute));

        if (attr != null)
        {
            switch (tableSqlInfos)
            {
                case TableSqlInfos.Name:
                    return ((TableAttribute)attr).Name;
                case TableSqlInfos.Schema:
                    return ((TableAttribute)attr).Schema ?? "dbo";
                default:
                    throw new ArgumentException(nameof(tableSqlInfos));
            }
        }

        throw new Exception("Error - construction object - no table associate");
    }

    /// <summary>
    /// Find Key using object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    internal static string FindKey<T>(this T obj)
    {
        return obj!
            .GetType()
            .FindKey();
    }

    /// <summary>
    /// Find Key using type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static string FindKey(this Type type)
    {
        var propertyAttributeKey = ((PropertyInfo[])((TypeInfo)type).DeclaredProperties).FirstOrDefault(p => p.GetCustomAttribute(typeof(KeyAttribute), true) != null);

        if (propertyAttributeKey != null)
            return propertyAttributeKey.Name;

        throw new Exception("Error - construction object - no key found");
    }

    /// <summary>
    /// Find column name
    /// </summary>
    /// <param name="type"></param>
    /// <param name="columnName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    internal static string FindColumnName(this Type type, string columnName)
    {
        var propertyAttributeColumn = ((PropertyInfo[])((TypeInfo)type).DeclaredProperties).FirstOrDefault(p => p.Name == columnName && p.GetCustomAttribute(typeof(ColumnAttribute), true) != null);

        if (propertyAttributeColumn != null)
            return propertyAttributeColumn.GetCustomAttribute<ColumnAttribute>().Name;

        throw new Exception("Error - construction object - no key found");
    }

    /// <summary>
    /// Set functional key
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TFunctionalKeyType"></typeparam>
    /// <param name="obj"></param>
    /// <param name="value"></param>
    /// <exception cref="Exception"></exception>
    internal static void SetfunctionalKey<T, TFunctionalKeyType>(ref T obj, TFunctionalKeyType value)
    {
        // Force cast/convert
        dynamic eo = JsonConvert.DeserializeObject<object>(JsonConvert.SerializeObject(obj))!;

        var propertyAttributeKey = ((PropertyInfo[])((TypeInfo)obj!.GetType()).DeclaredProperties).FirstOrDefault(p => p.GetCustomAttribute(typeof(KeyAttribute), true) != null);

        if (propertyAttributeKey != null)
        {
            // Change property (attribute : Key)
            eo[propertyAttributeKey.Name] = value;

            // Force cast/convert
            obj = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(eo))!;
        }
        else
            throw new Exception("Error - construction object");
    }

    /// <summary>
    /// Load date in satellite
    /// </summary>
    /// <param name="Satellite"></param>
    /// <param name="dto"></param>
    /// <param name="namespaceOfEntite"></param>
    internal static void ChargerSatellite(ref object Satellite, object dto, string namespaceOfEntite)
    {
        dynamic dynDto = dto;

        foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(Satellite))
        {
            if (!prop.PropertyType.FullName!.Contains(namespaceOfEntite))
            {
                SetObjectProperty(
                    ref Satellite,
                    prop.Name,
                    GetObjectProperty(dynDto, prop.Name),
                    false
                    );
            }
        }
    }

    /// <summary>
    /// Set property in object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="propertyTarget"></param>
    /// <param name="value"></param>
    /// <param name="allowNull"></param>
    internal static void SetObjectProperty<T>(ref T obj, string propertyTarget, object? value = null, bool allowNull = true)
    {
        if (value != null || (value == null && allowNull))
        {
            var eo = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(obj))!;
            if (value != null && value.GetType() == typeof(DateTime))
                eo[propertyTarget] = (DateTime)value;
            else
                eo[propertyTarget] = value?.ToString();

            // Force cast/convert
            obj = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(eo))!;
        }
    }

    /// <summary>
    /// Get instance object from a dll in another project, but the same solution
    /// </summary>
    /// <param name="fullName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    internal static object GetInstance(string? fullName)
    {
        if (fullName == null)
            throw new ArgumentNullException(nameof(fullName));

        var type = Assembly
            .GetEntryAssembly()?
            .GetTypes()?
            .Where(t => t.FullName == fullName)
            .First();

        return Activator.CreateInstance(type!)!;
    }

    /// <summary>
    /// Get target children (collection type) object
    /// </summary>
    /// <param name="T"></param>
    /// <param name="typeObject"></param>
    /// <returns></returns>
    internal static IEnumerable<Type> GetListOfChildrenObjects(this Type T, DV2TypeObject typeObject)
    {
        List<Type> list = [];

        foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(T))
        {
            if (
                prop.PropertyType.FullName!.Contains(T.Namespace!) &&
                (prop.PropertyType.Name == "ICollection`1") &&
                prop.Name.StartsWith(typeObject.ToString()))
            {
                list.Add(prop.PropertyType.GenericTypeArguments.First());
            }
        }

        return list;
    }
}