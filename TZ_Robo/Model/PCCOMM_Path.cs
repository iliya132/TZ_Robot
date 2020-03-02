using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TZ_Robo.Model
{
    [Table("PCCOMM_Path")]
    public class PCCOMM_Path
    {
        [Key]
        public int Id { get; set; }
        [Column("SessionName")]
        public string SessionName { get; set; }
        [Column("UserId")]
        public string User { get; set; }
        [Column("PCCOMM_Path")]
        public string Path { get; set; }
    }
}
