namespace CustomORM.Converter.Extensions;

public static class PropertiesExtractor
{
    public static string GetFunctionnalKeyProperty(List<string> hubProperties, List<string> viewProperties)
    {
        var commun = hubProperties.Intersect(viewProperties);

        return commun.First().ToString();
    }

    public static List<string> GetProperties(IEnumerable<string> lines)
    {
        List<string> properties = [];

        int numLineProperty = -1;

        do
        {
            numLineProperty = lines.ToList<string>().FindIndex(numLineProperty + 1, l => l.Contains("[Column(", StringComparison.OrdinalIgnoreCase));
            if (numLineProperty != -1)
            {
                var indexProperty = lines.ToList<string>().FindIndex(numLineProperty, l => !l.Contains("["));
                properties.Add(lines.ElementAt(indexProperty).Trim());
            }
        }
        while (numLineProperty != -1);

        return properties;
    }

    public static List<string> GetNoFunctionnalKeyProperties(List<string> hubProperties, List<string> viewProperties)
    {
        var functionalKeyProperty = GetFunctionnalKeyProperty(hubProperties, viewProperties);

        viewProperties.Remove(functionalKeyProperty);

        return viewProperties;
    }

    public static string? GetAnnotation(string property, IEnumerable<string> lines)
    {
        var numLineProperty = lines.ToList<string>().FindIndex(l => l.Contains(property, StringComparison.OrdinalIgnoreCase));

        var maybeAnnotationContent = lines.ElementAt(numLineProperty - 1).Trim();

        return maybeAnnotationContent.StartsWith("[Column(") ? null : maybeAnnotationContent;
    }
}
