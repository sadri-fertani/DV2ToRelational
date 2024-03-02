namespace CustomORM.Core.Abstractions;

internal interface IPit
{
    public DateTime PLoadDts { get; set; }

    public DateTime? PLoadEndDts { get; set; }

    public string PLoadUser { get; set; }

    public string PLoadSrc { get; set; }
}