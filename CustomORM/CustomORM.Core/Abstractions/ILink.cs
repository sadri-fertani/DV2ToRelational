namespace CustomORM.Core.Abstractions;

internal interface ILink
{
    public DateTime LLoadDts { get; set; }

    public string? LLoadUser { get; set; }

    public string? LLoadSrc { get; set; }
}