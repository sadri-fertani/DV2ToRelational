using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.Reflection;

namespace CustomORM.Core.Extensions
{
    public static class SpyIL
    {
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

        public static object ConvertToParamsRequest(this object obj)
        {
            ExpandoObject result = new ExpandoObject();

            var columns = obj.GetType().GetNamesColumns().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var column in columns)
            {
                var columnName = column.Substring(1);
                result.SetProperty
                    (
                        columnName,
                        obj.GetObjectProperty(columnName)!
                    );
            }

            return result;
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
            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(Type.GetType(obj.GetType().FullName)!))
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

            throw new Exception("Error - construction object");
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
    }
}
