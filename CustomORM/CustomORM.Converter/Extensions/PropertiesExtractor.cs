using System.Text.RegularExpressions;

namespace CustomORM.Converter.Extensions;

internal static class PropertiesExtractor
{
    internal static string GetFunctionnalKeyProperty(List<string> hubProperties, List<string> viewProperties)
    {
        var commun = hubProperties.Intersect(viewProperties);

        return commun.First().ToString();
    }

    internal static List<string> GetProperties(IEnumerable<string> lines)
    {
        List<string> properties = [];

        int numLineProperty = -1;

        do
        {
            numLineProperty = lines.ToList<string>().FindIndex(numLineProperty + 1, l => l.Contains("[Column(", StringComparison.OrdinalIgnoreCase));
            if (numLineProperty != -1)
            {
                var indexProperty = lines.ToList<string>().FindIndex(numLineProperty, l => !l.Contains("["));   // Find next line without annotation start
                properties.Add(lines.ElementAt(indexProperty).Trim());
            }
        }
        while (numLineProperty != -1);

        return properties;
    }

    internal static List<string> GetLinkedObject(IEnumerable<string> lines, string entitySource)
    {
        List<string> linkedObjects = [];

        foreach (var line in lines)
        {
            Match m = Regex.Match(line, "ICollection<L(.*?)>", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                // public virtual ICollection<LClientReclamation> LClientReclamations { get; set; } = new List<LClientReclamation>();
                var target = m.Groups[1].Value.Replace(entitySource, string.Empty);
                var targetProperty = target.EndsWith('s') ? target : $"{target}s";

                linkedObjects.Add($"public virtual ICollection<{target}> {targetProperty} {{ get; set; }} = new List<{target}>();");
            }
        }

        return linkedObjects;
    }

    internal static List<string> GetNoFunctionnalKeyProperties(List<string> hubProperties, List<string> viewProperties)
    {
        var functionalKeyProperty = GetFunctionnalKeyProperty(hubProperties, viewProperties);

        viewProperties.Remove(functionalKeyProperty);

        return viewProperties;
    }

    internal static string? GetAnnotation(string property, IEnumerable<string> lines)
    {
        var numLineProperty = lines.ToList<string>().FindIndex(l => l.Contains(property, StringComparison.OrdinalIgnoreCase));

        var maybeAnnotationContent = lines.ElementAt(numLineProperty - 1).Trim();

        return maybeAnnotationContent.StartsWith("[Column(") ? null : maybeAnnotationContent;
    }
}
