namespace CustomORM.Abstractions;

public interface ISattelite
{
    public string? SKeyHk { get; set; }
    public DateTime SLoadDts { get; set; }
    public string? SLoadUser { get; set; }
    public string? SLoadSrc { get; set; }
}
