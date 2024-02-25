namespace CustomORM.Abstractions
{
    public interface IAuditLoadInformations
    {
        public DateTime LoadDts { get; set; }
        public string LoadUser { get; set; }
        public string LoadSrc { get; set; }
    }
}
