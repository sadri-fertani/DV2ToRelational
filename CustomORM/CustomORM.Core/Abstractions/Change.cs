namespace CustomORM.Core.Abstractions;

internal class Change
{
    public string Satellite { get; set; }

    public string Property { get; set; }

    public object Value { get; set; }
}