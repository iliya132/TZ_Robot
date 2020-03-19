using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;
using TZ_Robo.Model.Entities;

namespace TZ_Robo.Model
{
    public class ExcelWorker
    {
        private FileInfo _file;

        public ExcelWorker(FileInfo file)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            _file = file;
        }

        public List<string> ReadForAccounts()
        {
            List<string> accountNumbers = new List<string>();
            using (ExcelPackage excel = new ExcelPackage(_file))
            {

                ExcelWorksheet sheet = excel.Workbook.Worksheets[0];
                for (int i = 2; i <= sheet.Dimension.Rows; i++)
                {
                    if (sheet.Cells[i, 1].Text.Length == 20)
                    {
                        accountNumbers.Add(sheet.Cells[i, 1].Text);
                    }
                }
            }
            return accountNumbers;
        }

        /// <summary>
        /// Прочитать excel файл и сгенерировать коллекцию операций
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<Operation> ReadForOperations()
        {
            string destinationDirectory = @"C:\AlfaBank\ArchiveTZ";
            string destinationFile = $@"{destinationDirectory}\temp.dbf";
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            if (File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
            }
            File.Move(_file.FullName, destinationFile);
            _file = new FileInfo(destinationFile);

            var dataTable = new DataTable();
            using (OdbcConnection obdcconn = new OdbcConnection())
            {
                obdcconn.ConnectionString = $"Driver={{Microsoft dBase Driver (*.dbf)}};SourceType=DBF;Data source ={_file.DirectoryName};Exclusive=No; NULL=NO;DELETED=NO;BACKGROUNDFETCH=NO;";
                obdcconn.Open();
                OdbcCommand oCmd = obdcconn.CreateCommand();
                oCmd.CommandText = $"SELECT SDZNEQ, SDZDTO, SDZBICP, SDZBNP, SDZCNMP, " +
                    $"SDZINNP, SDZEANP, SDZBICR, SDZBNR, SDZCUNR, SDZCRFR, SDZEANR, " +
                    $"SDZDT, SDZCR, SDZCMP1, SDZCMP2 FROM {_file.FullName}";
                dataTable.Load(oCmd.ExecuteReader());
            }

            if (File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
            }

            List<Operation> operations = new List<Operation>();

            for (int i = 0; i < dataTable.Rows.Count; i++)
            {
                if (!string.IsNullOrEmpty(dataTable.Rows[i]["SDZNEQ"].ToString()))
                {
                    Operation operation = new Operation
                    {
                        Number = dataTable.Rows[i]["SDZNEQ"].ToString(),
                        OperationDate = DateTime.Parse(dataTable.Rows[i]["SDZDTO"].ToString()),
                        PayerBankBIK = dataTable.Rows[i]["SDZBICP"].ToString(),
                        PayerBankName = dataTable.Rows[i]["SDZBNP"].ToString(),
                        PayerName = dataTable.Rows[i]["SDZCNMP"].ToString(),
                        PayerINN = dataTable.Rows[i]["SDZINNP"].ToString(),
                        PayerAccountNumber = dataTable.Rows[i]["SDZEANP"].ToString(),
                        RecieverBankBIK = dataTable.Rows[i]["SDZBICR"].ToString(),
                        RecieverBankName = dataTable.Rows[i]["SDZBNR"].ToString(),
                        RecieverName = dataTable.Rows[i]["SDZCUNR"].ToString(),
                        RecieverINN = dataTable.Rows[i]["SDZCRFR"].ToString(),
                        RecieverAccount = dataTable.Rows[i]["SDZEANR"].ToString(),
                        DebetSum = dataTable.Rows[i]["SDZDT"].ToString(),
                        CreditSum = dataTable.Rows[i]["SDZCR"].ToString(),
                        Comment = dataTable.Rows[i]["SDZCMP1"].ToString(),
                        Comment2 = dataTable.Rows[i]["SDZCMP2"].ToString(),
                    };
                    operations.Add(operation);
                }
                else
                {
                    operations.Add(new Operation { Number = string.Empty});
                }
            }
            return operations;
        }

        internal void GenerateTz(IEnumerable<Operation> operations, string fileName)
        {
            using (ExcelPackage excel = new ExcelPackage())
            {
                ExcelWorksheet sheet = excel.Workbook.Worksheets.Add("sheet1");
                int row = 2;

                #region placeHeaders
                sheet.Cells[1, 1].Value = "№п/п";
                sheet.Cells[1, 2].Value = "Дата операции";
                sheet.Column(2).Style.Numberformat.Format = "dd.MM.yyyy";
                sheet.Cells[1, 3].Value = "БИК банка плательщика";
                sheet.Cells[1, 4].Value = "Банк плательщика";
                sheet.Cells[1, 5].Value = "Плательщик";
                sheet.Cells[1, 6].Value = "ИНН плательщика";
                sheet.Cells[1, 7].Value = "Счет плательщика";
                sheet.Cells[1, 8].Value = "БИК банка получателя";
                sheet.Cells[1, 9].Value = "Банк получателя";
                sheet.Cells[1, 10].Value = "Получатель";
                sheet.Cells[1, 11].Value = "ИНН получателя";
                sheet.Cells[1, 12].Value = "Счет получателя";
                sheet.Cells[1, 13].Value = "Сумма Дт";
                sheet.Cells[1, 14].Value = "Сумма Кт";
                sheet.Cells[1, 15].Value = "Назначение1";
                sheet.Cells[1, 16].Value = "Назначение2";
                #endregion

                #region placeData
                foreach (Operation operation in operations)
                {
                    int col = 1;
                    sheet.Cells[row, col++].Value = operation.Number;
                    sheet.Cells[row, col++].Value = operation.OperationDate;
                    sheet.Cells[row, col++].Value = operation.PayerBankBIK;
                    sheet.Cells[row, col++].Value = operation.PayerBankName;
                    sheet.Cells[row, col++].Value = operation.PayerName;
                    sheet.Cells[row, col++].Value = operation.PayerINN;
                    sheet.Cells[row, col++].Value = operation.PayerAccountNumber;
                    sheet.Cells[row, col++].Value = operation.RecieverBankBIK;
                    sheet.Cells[row, col++].Value = operation.RecieverBankName;
                    sheet.Cells[row, col++].Value = operation.RecieverName;
                    sheet.Cells[row, col++].Value = operation.RecieverINN;
                    sheet.Cells[row, col++].Value = operation.RecieverAccount;
                    sheet.Cells[row, col++].Value = operation.DebetSum;
                    sheet.Cells[row, col++].Value = operation.CreditSum;
                    sheet.Cells[row, col++].Value = operation.Comment;
                    sheet.Cells[row, col++].Value = operation.Comment2;
                    if (operation.IsEdited)
                    {
                        sheet.Row(row).Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        sheet.Row(row).Style.Fill.BackgroundColor.SetColor(Color.Orange);
                    }
                    row++;
                }
                #endregion

                excel.SaveAs(new FileInfo($"{fileName}"));
            }
        }
    }
}
