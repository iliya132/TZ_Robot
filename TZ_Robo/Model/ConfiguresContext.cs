using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TZ_Robo.Model
{
    public class ConfiguresContext : DbContext
    {
        public ConfiguresContext() : base("TZ_ConfigDB")
        { }
        public DbSet<Unit> UnitsSet { get; set; }
        public DbSet<PCCOMM_Path> PCCOMM_Paths { get; set; }
    }
}
