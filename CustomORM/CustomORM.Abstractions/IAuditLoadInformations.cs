namespace CustomORM.Abstractions
{
    public interface IHubAuditLoadInformations
    {
        public DateTime HLoadDts { get; set; }
        public string HLoadUser { get; set; }
        public string HLoadSrc { get; set; }
    }
}
