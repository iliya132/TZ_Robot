using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TZ_Robo.Model
{
    [Table("UnitsTable")]
    public class Unit
    {
        public Unit()
        {
            DateStart = DateTime.Now.AddYears(-1);
            DateEnd = DateTime.Now;
        }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column("Name")]
        public string Name { get; set; }
        [Column("StartDate")]
        public DateTime DateStart { get; set; }
        [Column("EndDate")]
        public DateTime DateEnd { get; set; }
    }
}
