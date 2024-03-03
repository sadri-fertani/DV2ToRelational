// <auto-generated> This file has been auto generated by CustomORM.Converter. </auto-generated>
#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CustomORM.Console.Entities.Relationals;

public partial class Client
{
    [Key]
    public int NoClient { get; set; }

    [StringLength(20)]
    public string Prenom { get; set; }

    [StringLength(20)]
    public string Nom { get; set; }

    [StringLength(1)]
    public string Sexe { get; set; }

    public DateTime? DateNaissance { get; set; }

    public DateTime? DateDeces { get; set; }

    [StringLength(3)]
    public string Langue { get; set; }

    public string Adresse1 { get; set; }

    public string Adresse2 { get; set; }

    public string Adresse3 { get; set; }

    [StringLength(100)]
    public string Cite { get; set; }

    [StringLength(2)]
    public string Provence { get; set; }

    [StringLength(2)]
    public string Pays { get; set; }

    [StringLength(16)]
    public string CodePostale { get; set; }

    public virtual ICollection<Reclamation> Reclamations { get; set; } = new List<Reclamation>();
}

