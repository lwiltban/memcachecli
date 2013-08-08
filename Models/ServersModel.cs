using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MinimalisticTelnet;

namespace MemcacheCLI.Models
{
    public class ServersModel
    {
        public virtual ICollection<Server> Servers { get; set; }
    }
}