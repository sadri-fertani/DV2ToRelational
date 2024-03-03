using System.Text;

namespace CustomORM.Converter.Extensions;

internal static class StringBuilderExtensions
{
    internal static StringBuilder AppendIndentedLine(this StringBuilder builder, string content)
    {
        return builder.AppendLine($"    {content}");
    }
}