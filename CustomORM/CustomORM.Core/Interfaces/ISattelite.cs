using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomORM.Core.Interfaces;

public interface ISattelite
{
    public DateTime SLoadDts { get; set; }
    public string? SLoadUser { get; set; }
    public string? SLoadSrc { get; set; }
}
