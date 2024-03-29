﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomORM.Console.Entities.DV2;

[Table("h_reclamation")]
public partial class HReclamation
{
    [Key]
    [Column("h_reclamation_hk")]
    [StringLength(64)]
    public string HReclamationHk { get; set; }

    [Column("h_load_dts", TypeName = "datetime")]
    public DateTime HLoadDts { get; set; }

    [Required]
    [Column("h_load_user")]
    [StringLength(10)]
    public string HLoadUser { get; set; }

    [Required]
    [Column("h_load_src")]
    [StringLength(10)]
    public string HLoadSrc { get; set; }

    [Column("no_reclamation")]
    public int NoReclamation { get; set; }

    [InverseProperty("HReclamationHkNavigation")]
    public virtual ICollection<LClientReclamation> LClientReclamations { get; set; } = new List<LClientReclamation>();

    [InverseProperty("HReclamationHkNavigation")]
    public virtual ICollection<PReclamation> PReclamations { get; set; } = new List<PReclamation>();

    [InverseProperty("HReclamationHkNavigation")]
    public virtual ICollection<SReclamationInfo> SReclamationInfos { get; set; } = new List<SReclamationInfo>();
}