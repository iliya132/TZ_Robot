using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace TZ_Robo.Model
{
    public class ExcelWorker
    {
        private readonly FileInfo _file;

        public ExcelWorker(FileInfo file)
        {
            _file = file;
        }

        public List<string> ReadForAccounts()
        {
            List<string> accountNumbers = new List<string>();
            using (ExcelPackage excel = new ExcelPackage(_file))
            {
                ExcelWorksheet sheet = excel.Workbook.Worksheets[1];
                for (int i = 2; i <= sheet.Dimension.Rows; i++)
                {
                    accountNumbers.Add(sheet.Cells[i, 1].Text);
                }
            }
            return accountNumbers;
        }
    }
}
