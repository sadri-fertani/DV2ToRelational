namespace CustomORM.Core.Abstractions;

internal interface ISatellite
{
    public DateTime SLoadDts { get; set; }

    public string? SLoadUser { get; set; }

    public string? SLoadSrc { get; set; }
}