using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CustomORM.Core.Extensions
{
    public static class SpyIL
    {
        public static Dictionary<string, string> GetMappingNamesColumnsProperties(this Type className)
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

        public static string GetNamesColumns(this Type className)
        {
            var namespaceOfEntite = className.Namespace;
            List<string> columnsNames = new List<string>();

            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(className))
            {
                if (!prop.PropertyType.FullName!.Contains(namespaceOfEntite!))
                {
                    var attributeColumn = prop.Attributes[typeof(ColumnAttribute)] as ColumnAttribute;

                    if (attributeColumn != null)
                        columnsNames.Add($"@{attributeColumn.Name}");
                    else
                        columnsNames.Add($"@{prop.Name}");
                }
            }

            return string.Join(",", columnsNames);
        }

        public static DynamicParameters ConvertToParamsRequest(this object obj)
        {
            var dbArgs = new DynamicParameters();

            var mapping = obj.GetType().GetMappingNamesColumnsProperties();

            var columns = obj.GetType().GetNamesColumns().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var column in columns)
            {
                var columnName = column.Substring(1);
                var propertyTarget = mapping.FirstOrDefault(x => x.Value == columnName).Key;

                dbArgs.Add($"{columnName}", obj.GetValue(propertyTarget)!.ToString());
            }
            
            return dbArgs;
        }

        public static object GetValue(this object obj, string propertyName)
        {
            dynamic dyn = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(obj))!;

            return dyn[propertyName];
        }

        /// <summary>
        /// Injecter ou update une propriete dans un objet
        /// </summary>
        /// <param name="expando"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        private static void SetProperty(this ExpandoObject expando, string propertyName, object propertyValue)
        {
            var expandoDict = expando as IDictionary<string, object>;

            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }

        public static object? GetObjectProperty(this object obj, string propertyName)
        {
            var name = FindPropertyByAttribute(obj, propertyName);

            PropertyInfo propertyInfo = obj.GetType().GetProperty(name)!;

            return propertyInfo != null ?
                propertyInfo.GetValue(obj, null) :
                null;
        }

        public static string FindPropertyByAttribute(object obj, string propertyName)
        {
            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(Type.GetType(obj.GetType().FullName!)!))
            {
                if (!prop.PropertyType.FullName!.Contains(obj.GetType().Namespace!))
                {
                    var attributeColumn = prop.Attributes[typeof(ColumnAttribute)] as ColumnAttribute;

                    if (attributeColumn != null)
                    {
                        if (propertyName == attributeColumn.Name)
                            return prop.Name;
                    }
                }
            }

            string msg = $"Error - construction object - property {propertyName} not found";

            Log.Fatal(msg);
            Console.WriteLine(msg);
            throw new Exception(msg);
        }

        public static string FindTableTarget(this Type obj)
        {
            var attr = obj.GetCustomAttribute(typeof(TableAttribute));

            if (attr != null)
                return ((TableAttribute)attr).Name;

            throw new Exception("Error - construction object - no table associate");
        }

        public static string FindKey<T>(this T obj)
        {
            var propertyAttributeKey = ((PropertyInfo[])((TypeInfo)obj!.GetType()).DeclaredProperties).FirstOrDefault(p => p.GetCustomAttribute(typeof(KeyAttribute), true) != null);

            if (propertyAttributeKey != null)
                return propertyAttributeKey.Name;

            throw new Exception("Error - construction object - no key found");
        }

        public static void SetFunctionnalKey<T>(ref T obj, int value)
        {
            // Force cast/convert
            dynamic eo = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(obj))!;

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

        public static void ChargerSatellite(object Satellite, object hc, object dto, string namespaceOfEntite)
        {
            dynamic dynSat = Satellite;
            dynamic dynDto = dto;

            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(Satellite))
            {
                if (!prop.PropertyType.FullName!.Contains(namespaceOfEntite))
                {
                    SetObjectProperty(
                        prop.Name,
                        GetObjectProperty(dynDto, prop.Name),
                        Satellite);
                }
            }
        }

        private static void SetObjectProperty(string propertyName, object value, object obj)
        {
            PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName)!;

            if (propertyInfo != null)
                propertyInfo.SetValue(obj, value, null);
        }

        public static void SetAuditInfo<T>(ref T obj, string propertyTarget, object value)
        {
            var eo = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(obj))!;
            eo[propertyTarget] =value.ToString();

            // Force cast/convert
            obj = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(eo))!;
        }

        public static object GetInstance(string fullName)
        {
            var types = Assembly.GetEntryAssembly().GetTypes();
            var filteredType = types.Where(t => t.FullName == fullName).First();
            
            return Activator.CreateInstance(filteredType);
        }
    }
}
