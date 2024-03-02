namespace CustomORM.Core.Abstractions;

internal interface IHub
{
    public DateTime HLoadDts { get; set; }

    public string? HLoadUser { get; set; }

    public string? HLoadSrc { get; set; }
}