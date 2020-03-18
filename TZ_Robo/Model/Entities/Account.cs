using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TZ_Robo.Model.Entities
{
    public class Account
    {
        public string Number { get; set; }
        public DateTime OpenedDate { get; set; }
        public DateTime ClosedDate { get; set; }
        private List<Unit> _units = new List<Unit>();
        public List<Unit> Units { get=>_units; set=>_units=value; }
    }
}
