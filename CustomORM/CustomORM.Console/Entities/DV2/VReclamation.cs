﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CustomORM.Console.Entities.DV2;

[Keyless]
public partial class VReclamation
{
    [Column("no_reclamation")]
    public int NoReclamation { get; set; }

    [Column("contenu")]
    public string Contenu { get; set; }

    [Column("priorite")]
    [StringLength(1)]
    public string Priorite { get; set; }
}