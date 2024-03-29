﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomORM.Console.Entities.DV2;

[PrimaryKey("HClientHk", "PLoadDts")]
[Table("p_client")]
public partial class PClient
{
    [Key]
    [Column("h_client_hk")]
    [StringLength(64)]
    public string HClientHk { get; set; }

    [Key]
    [Column("p_load_dts", TypeName = "datetime")]
    public DateTime PLoadDts { get; set; }

    [Column("p_load_end_dts", TypeName = "datetime")]
    public DateTime? PLoadEndDts { get; set; }

    [Required]
    [Column("p_load_user")]
    [StringLength(10)]
    public string PLoadUser { get; set; }

    [Required]
    [Column("p_load_src")]
    [StringLength(30)]
    public string PLoadSrc { get; set; }

    [Column("s_identification_ldts", TypeName = "datetime")]
    public DateTime? SIdentificationLdts { get; set; }

    [Column("s_adresse_ldts", TypeName = "datetime")]
    public DateTime? SAdresseLdts { get; set; }

    [ForeignKey("HClientHk")]
    [InverseProperty("PClients")]
    public virtual HClient HClientHkNavigation { get; set; }

    [ForeignKey("HClientHk, SAdresseLdts")]
    [InverseProperty("PClients")]
    public virtual SClientAdresse SClientAdresse { get; set; }

    [ForeignKey("HClientHk, SIdentificationLdts")]
    [InverseProperty("PClients")]
    public virtual SClientIdentification SClientIdentification { get; set; }
}