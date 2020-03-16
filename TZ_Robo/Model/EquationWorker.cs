using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public EquationWorker(PCCOMM_Path alfamos2, PCCOMM_Path alfamos4) {
            _alfamos2 = alfamos2;
            _alfamos4 = alfamos4;
        }

        public char getConnectionTest()
        {
            return OpenConnection(_alfamos4);
        }

        public void GetAccountInfo(Account account)
        {
            OpenConnection(_alfamos4);

        }

        private void runAlfamos(char currentSessionName, string filePath)
        {
            int count = 0;
            Process.Start(filePath);
            while (EUCL.Connect($"{currentSessionName}") != 0)
            {
                Thread.Sleep(1000);
                count++;
                if (count > 10) throw new Exception("Не удалось запустить сессию Equation.");
            }
            EUCL.Connect($"{currentSessionName}");
            enter();
            enter();
            enter();
            EUCL.Disconnect(char.ToString(currentSessionName));
        }

        private char OpenConnection(PCCOMM_Path anyAlfamos)
        {
            char currentSessionName = 'H';
            for (int i = 8; i > 0; i--)
            {
                if (EUCL.Connect($"{ currentSessionName }") == 0)
                {
                    currentSessionName++;
                    runAlfamos(currentSessionName, anyAlfamos.Path);
                    return currentSessionName;
                }
                else if (currentSessionName != 'A') //BCDEFGHIJK
                {
                    currentSessionName--;
                }
                else //A
                {
                    runAlfamos(currentSessionName, anyAlfamos.Path);
                }
            }
            return currentSessionName;
        }
        private void enter()
        {
            EUCL.Wait();
            EUCL.SendStr("@E");
            EUCL.Wait();
        }
        private void send(string text, int x, int y, int size=80)
        {
            EUCL.SetCursorPos(x, y, size);
            EUCL.SendStr(text);
            EUCL.Wait();
        }

        internal void FillAccounts(List<Account> accounts, char connectionChar)
        {
            EUCL.Connect($"{connectionChar}");
            foreach(Account acc in accounts)
            {
                EUCL.ClearScreen();
                send("TZ", 21, 17);
                enter();
                send(acc.Number, 3, 29);

            }
            EUCL.Disconnect($"{connectionChar}");
        }

    }
}
