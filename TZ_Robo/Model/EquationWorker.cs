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

        public EquationWorker(PCCOMM_Path alfamos2, PCCOMM_Path alfamos4, PCCOMM_Path pcscm) {
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
            if(Process.GetProcessesByName("pcscm.exe").Length<1)
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
                .Replace(",",string.Empty)
                .Replace('.',','); //сумма дебет
            string sumCdtStr = EUCL.ReadScreen(row, 58, 20).Replace(" ", string.Empty)
                .Replace(",", string.Empty)
                .Replace('.', ','); //сумма дебет

            double.TryParse(sumDbtStr, out double sumDbrDbl);
            double.TryParse(sumCdtStr, out double sumCdrDbl);
            EUCL.ClearScreen();
            if (sumDbrDbl==0 && sumCdrDbl == 0) //оборота нет - просто выходим
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
        public List<Operation> GetOperations(Account account, Process alfamos)
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
            alfamos.CloseMainWindow();

            #endregion

            #region ждем готового файла
            if (!Directory.Exists(_fileExportPath))
            {
                throw new Exception($@"У пользователя отсутствует директория экспорта на {_fileExportPath}");
            }
            for (int i = 0; i < WAIT_TIME; i++)
            {
                bool done = false;
                foreach(string fileName in Directory.GetFiles(_fileExportPath, "*.DBF"))
                {
                    FileInfo file = new FileInfo(fileName);
                    if(file.CreationTime > queryDateTime)
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
        private void send(string text, int x, int y, int size=80)
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
            foreach(Account acc in accounts)
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
            _currentUserProfile = EUCL.ReadScreen(6,18,4);
            EUCL.SendStr("@c");
            EUCL.Wait();
            EUCL.Disconnect(connectionChar.ToString());
        }
    }
}
