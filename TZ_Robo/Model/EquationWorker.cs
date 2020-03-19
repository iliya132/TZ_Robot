using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EquationLibrary;
using TZ_Robo.Model.Entities;

namespace TZ_Robo.Model
{
    public class EquationWorker
    {
        private readonly PCCOMM_Path _alfamos2;
        private readonly PCCOMM_Path _alfamos4;
        private readonly PCCOMM_Path _pcscm;
        private string _fileExportPath;
        private string _currentUserProfile;
        const int WAIT_TIME = 10;

        public EquationWorker(PCCOMM_Path alfamos2, PCCOMM_Path alfamos4, PCCOMM_Path pcscm)
        {
            _alfamos2 = alfamos2;
            _alfamos4 = alfamos4;
            _pcscm = pcscm;
        }

        /// <summary>
        /// запустить сессию pccomm
        /// </summary>
        /// <param name="currentSessionName"></param>
        /// <param name="filePath"></param>
        private Process runAlfamos(char currentSessionName, string filePath)
        {
            int count = 0;
            if (Process.GetProcessesByName("pcscm").Length < 1)
            {
                Process.Start(_pcscm.Path);
            }
            Process newAlfamos = Process.Start(filePath);
            while (EUCL.Connect($"{currentSessionName}") != 0)
            {
                count++;
                if (count > WAIT_TIME) throw new Exception("Не удалось запустить сессию Equation.");
                Thread.Sleep(1000);
            }
            EUCL.Connect($"{currentSessionName}");
            enter();
            enter();
            enter();
            return newAlfamos;
        }

        /// <summary>
        /// Запустить alfamos и установить подключение
        /// </summary>
        /// <param name="anyAlfamos"></param>
        /// <returns>имя новой сессии</returns>
        public char OpenConnection(PCCOMM_Path anyAlfamos, out Process alfamos)
        {
            char currentSessionName = 'H';
            alfamos = null;
            for (int i = 8; i > 0; i--)
            {
                if (EUCL.Connect($"{ currentSessionName }") == 0)
                {
                    currentSessionName++;
                    alfamos = runAlfamos(currentSessionName, anyAlfamos.Path);

                    return currentSessionName;
                }
                else if (currentSessionName != 'A')
                {
                    currentSessionName--;
                }
                else
                {
                    alfamos = runAlfamos(currentSessionName, anyAlfamos.Path);
                }
            }
            return currentSessionName;
        }

        /// <summary>
        /// Проверить на наличие оборота
        /// </summary>
        /// <param name="account"></param>
        /// <returns>True в случае, если оборот > 0, False, если == 0 </returns>
        public bool CheckForTurnover(Account account)
        {
            EUCL.ClearScreen();
            send("TOO", 21, 17);
            enter();
            send(account.Number, 3, 29);
            enter();
            int row = 0;
            for (int i = 7; i < 22; i++)
            {
                if (EUCL.ReadScreen(i, 8, 6).Equals("ИТОГО:"))
                {
                    row = i;
                    break;
                }
            }
            if (row == 0) throw new Exception("Необработанное исключение"); //TODO обработать

            string sumDbtStr = EUCL.ReadScreen(row, 33, 20).Replace(" ", string.Empty)
                .Replace(",", string.Empty)
                .Replace('.', ','); //сумма дебет
            string sumCdtStr = EUCL.ReadScreen(row, 58, 20).Replace(" ", string.Empty)
                .Replace(",", string.Empty)
                .Replace('.', ','); //сумма дебет

            double.TryParse(sumDbtStr, out double sumDbrDbl);
            double.TryParse(sumCdtStr, out double sumCdrDbl);
            EUCL.ClearScreen();
            if (sumDbrDbl == 0 && sumCdrDbl == 0) //оборота нет - просто выходим
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void EnterUnit(Unit unit)
        {
            _fileExportPath = $@"\\alfamos2\ALFAPRODDATA\{unit.Name}\REPORTING\{_currentUserProfile}";
            send(unit.Name, 21, 18);
            enter();
            enter();
            enter();
            EUCL.ClearScreen();
        }

        /// <summary>
        /// Запросить TZ в Equation
        /// </summary>
        /// <param name="account">Счет, для которого получаем выписку</param>
        /// <param name="alfamos">сессия, которая будет закрыта</param>
        /// <returns></returns>
        public List<Operation> GetOperations(Account account, Process alfamos, Unit unit)
        {
            List<Operation> exportedOperations = new List<Operation>();

            #region направляем в EQ запрос на создание выписки
            DateTime queryDateTime = DateTime.Now;
            EUCL.ClearScreen();
            send("TZ", 21, 17);
            enter();
            send(account.Number, 3, 29);
            enter();
            EUCL.SendStr("@4"); //F4
            EUCL.Wait();
            enter();


            #endregion

            #region ждем готового файла
            if (!Directory.Exists(_fileExportPath))
            {
                throw new Exception($@"У пользователя отсутствует директория экспорта на {_fileExportPath}");
            }
            for (int i = 0; i < WAIT_TIME; i++)
            {
                bool done = false;
                foreach (string fileName in Directory.GetFiles(_fileExportPath, "*.DBF"))
                {
                    FileInfo file = new FileInfo(fileName);
                    if (file.CreationTime > queryDateTime)
                    {
                        ExcelWorker excel = new ExcelWorker(file);
                        exportedOperations.AddRange(excel.ReadForOperations());
                        done = true;
                        break;
                    }
                }
                if (done) break;
                Thread.Sleep(5000);
            }
            #endregion

            #region Поиск недостающих операций
            for (int i = 0; i < exportedOperations.Count; i++)
            {

                if (string.IsNullOrWhiteSpace(exportedOperations[i].Number) || exportedOperations[i].OperationDate.Year < 1950)
                {
                    DateTime LastFullDate; // дата последней операции  полными данными
                    if (i != 0)
                    {

                        LastFullDate = exportedOperations[i - 1].OperationDate;
                    }
                    else
                    {

                        LastFullDate = account.OpenedDate < unit.DateStart ? unit.DateStart : account.OpenedDate;
                    }

                    DateTime NextFullDate = unit.DateEnd; // дата следующей заполненной опреации
                    for (int j = i; j < exportedOperations.Count; j++)
                    {
                        if (!string.IsNullOrEmpty(exportedOperations[j].Number))
                        {
                            NextFullDate = exportedOperations[j].OperationDate > unit.DateEnd ? unit.DateEnd : exportedOperations[j].OperationDate;
                            break;
                        }
                        if (j == exportedOperations.Count - 1)
                        {
                            if(account.ClosedDate < unit.DateEnd && account.ClosedDate > unit.DateStart)
                            {
                                NextFullDate = account.ClosedDate;
                            }
                            else
                            {
                                NextFullDate = unit.DateEnd;
                            }
                        }
                    }

                    EUCL.ClearScreen();
                    send("TT", 21, 17);
                    enter();
                    send(account.Number, 3, 29);
                    send(LastFullDate.ToString("ddMMyy"), 14, 29);
                    send(NextFullDate.ToString("ddMMyy"), 16, 29);
                    enter();
                    enter();
                    #region поиск операций
                    do
                    {
                        EUCL.SendStr("1111111111");
                        EUCL.SendStr("@v");
                        EUCL.Wait();
                        EUCL.SendStr("1111111111");
                    } while (EUCL.ReadScreen(21, 79, 1).Equals("+"));
                    enter();
                    while(EUCL.ReadScreen(1,32,17).Equals("Просмотр проводки"))
                    {
                        string currentOperationNumber = EUCL.ReadScreen(8, 29, 45).Replace(" ", string.Empty);
                        if (string.IsNullOrWhiteSpace(currentOperationNumber) || !exportedOperations.Any(operation => operation.Number.Equals(currentOperationNumber)) &&
                            !exportedOperations[i].IsEdited)
                        {
                            Operation newOperation = new Operation
                            {
                                Number = currentOperationNumber,
                                OperationDate = DateTime.Parse(EUCL.ReadScreen(3, 40, 11)),
                                PayerAccountNumber = EUCL.ReadScreen(9, 15, 24).Replace(".", string.Empty),
                                RecieverAccount = EUCL.ReadScreen(12, 15, 24).Replace(".", string.Empty),
                                PayerName = $"{EUCL.ReadScreen(9, 44, 35).Replace("  ", string.Empty)}{EUCL.ReadScreen(10, 44, 35).Replace("  ", string.Empty)}{EUCL.ReadScreen(11, 44, 35).Replace("  ", string.Empty)}",
                                RecieverName = $"{EUCL.ReadScreen(12, 44, 35).Replace("  ", string.Empty)}{EUCL.ReadScreen(13, 44, 35).Replace("  ", string.Empty)}{EUCL.ReadScreen(14, 44, 35).Replace("  ", string.Empty)}",
                                Comment = $"{EUCL.ReadScreen(18, 10, 70).Replace("  ", string.Empty)}{EUCL.ReadScreen(19, 10, 70).Replace("  ", string.Empty)}",
                                DebetSum = EUCL.ReadScreen(15, 15, 20).Replace(" ", string.Empty),
                                IsEdited = true
                            };
                            if (newOperation.RecieverAccount.Equals("40911810904000000138") ||
                                newOperation.Comment.IndexOf("перевод между счетами") > -1)
                            {
                                newOperation.RecieverBankBIK = newOperation.PayerBankBIK = "044525593";
                                newOperation.RecieverBankName = newOperation.PayerBankName = "АО \"Альфа-Банк\"";
                            }
                            exportedOperations[i] = newOperation;
                            break;
                        }
                        enter();
                        enter();
                    }
                    #endregion
                }
            }
            #endregion
            
            alfamos.CloseMainWindow();
            alfamos.WaitForExit(10000);
            return exportedOperations;
        }

        /// <summary>
        /// Отправить enter в текущую сессию
        /// </summary>
        private void enter()
        {
            EUCL.Wait();
            EUCL.SendStr("@E");
            EUCL.Wait();
        }

        /// <summary>
        /// Отправить 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="size"></param>
        private void send(string text, int x, int y, int size = 80)
        {
            EUCL.SetCursorPos(x, y, size);
            EUCL.SendStr(text);
            EUCL.Wait();
        }

        /// <summary>
        /// Заполнить коллекцию счетов данными из equation
        /// </summary>
        /// <param name="accounts"></param>
        /// <param name="connectionChar"></param>
        internal void FillAccounts(List<Account> accounts, char connectionChar)
        {
            EUCL.Connect($"{connectionChar}");
            foreach (Account acc in accounts)
            {
                DateTime temp;
                EUCL.ClearScreen();
                send("БРХ", 21, 17);
                enter();
                send(acc.Number, 3, 29);
                enter();
                DateTime.TryParse(EUCL.ReadScreen(4, 70, 11), out temp);
                acc.OpenedDate = temp;
                if (DateTime.TryParse(EUCL.ReadScreen(5, 70, 11), out temp))
                {
                    acc.ClosedDate = temp;
                }
            }
            EUCL.ClearScreen();
            EUCL.Disconnect($"{connectionChar}");
        }

        /// <summary>
        /// Записать текущий профайл пользователя в Equation
        /// </summary>
        /// <param name="connectionChar">имя сессии</param>
        internal void GetUserProfile(char connectionChar)
        {
            EUCL.Connect(connectionChar.ToString());
            EUCL.ClearScreen();
            send("Я", 21, 17);
            enter();
            _currentUserProfile = EUCL.ReadScreen(6, 18, 4);
            EUCL.SendStr("@c");
            EUCL.Wait();
            EUCL.Disconnect(connectionChar.ToString());
        }
    }
}
