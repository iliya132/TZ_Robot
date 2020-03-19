using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TZ_Robo.Model.Entities
{
    public class Operation
    {
        public string Number { get; set; }
        public DateTime OperationDate { get; set; }
        public string PayerBankBIK { get; set; }
        public string PayerBankName { get; set; }
        public string PayerName { get; set; }
        public string PayerINN { get; set; }
        public string PayerAccountNumber { get; set; }
        public string RecieverBankBIK { get; set; }
        public string RecieverBankName { get; set; }
        public string RecieverName { get; set; }
        public string RecieverINN { get; set; }
        public string RecieverAccount { get; set; }
        public string DebetSum { get; set; }
        public string CreditSum { get; set; }
        public string Comment { get; set; }
        public string Comment2 { get; set; }
        public bool IsEdited { get; set; }
    }
}
