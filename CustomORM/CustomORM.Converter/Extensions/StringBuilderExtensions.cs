using System.Text;

namespace CustomORM.Converter.Extensions;

public static class StringBuilderExtensions
{
    public static StringBuilder AppendIndentedLine(this StringBuilder builder, string content)
    {
        return builder.AppendLine($"    {content}");
    }
}