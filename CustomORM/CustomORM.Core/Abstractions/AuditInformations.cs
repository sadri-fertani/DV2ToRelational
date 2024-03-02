namespace CustomORM.Core.Abstractions;

internal class AuditInformations
{
    public DateTime LoadDts { get; set; } = DateTime.Now;

    public string LoadUser { get; set; } = Environment.UserName;

    public string LoadSrc { get; set; } = AppDomain.CurrentDomain.FriendlyName;
}
