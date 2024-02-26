using System.ComponentModel.DataAnnotations;

namespace CustomORM.Console.Entities.Relationals;

public sealed class Client
{
    [Key]
    public int? NoClient { get; set; }

    public string? Adresse1 { get; set; }

    public string? Adresse2 { get; set; }

    public string? Adresse3 { get; set; }

    public string? Cite { get; set; }

    public string? Provence { get; set; }

    public string? Pays { get; set; }

    public string? CodePostale { get; set; }

    public string? Prenom { get; set; }

    public string? Nom { get; set; }

    public string? Sexe { get; set; }

    public DateTime? DateNaissance { get; set; }

    public DateTime? DateDeces { get; set; }

    public string? Langue { get; set; }
}
