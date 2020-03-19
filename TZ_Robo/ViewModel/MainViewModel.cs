using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using TZ_Robo.Model;
using TZ_Robo.Model.Entities;

namespace TZ_Robo.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        #region Collections
        private ObservableCollection<Unit> _units;
        public ObservableCollection<Unit> Units { get => _units; set => _units = value; }

        private ObservableCollection<PCCOMM_Path> _paths;
        public ObservableCollection<PCCOMM_Path> Paths { get => _paths; set => _paths = value; }
        #endregion

        #region CurrentValues
        private string _userInput { get; set; }
        public string UserInput { get => _userInput; set => _userInput = value; }
        private Unit _userInputUnit = new Unit();
        public Unit UserInputUnit { get => _userInputUnit; set => _userInputUnit = value; }
        public string oldUnitName { get; set; }
        public string Log { get; set; }
        #endregion

        DataProvider DataProvider = new DataProvider();

        #region Commands
        public RelayCommand AddAlfamos2Command { get; set; }
        public RelayCommand AddAlfamos4Command { get; set; }
        public RelayCommand Upload { get; set; }
        public RelayCommand AddUnit { get; set; }
        public RelayCommand AddPCSCM { get; set; }
        public RelayCommand<Unit> DeleteUnit { get; set; }
        public RelayCommand<Unit> EditUnit { get; set; }

        #endregion

        public MainViewModel()
        {
            CollectionsInit();
            CommandsInit();
            Log = "Готов к работе";
        }

        #region Initialization

        /// <summary>
        /// Инициализация всех команд
        /// </summary>
        private void CommandsInit()
        {
            AddAlfamos2Command = new RelayCommand(() => AddAlfamos(DataProvider.SesionName.Alfamos2));
            AddAlfamos4Command = new RelayCommand(() => AddAlfamos(DataProvider.SesionName.Alfamos4));
            AddPCSCM = new RelayCommand(() => AddAlfamos(DataProvider.SesionName.PCSCM));

            Upload = new RelayCommand(GetReport);

            AddUnit = new RelayCommand(() =>
            {
                InputUnitBox inputBox = new InputUnitBox();
                if (inputBox.ShowDialog() == true && !string.IsNullOrEmpty(UserInputUnit.Name))
                {
                    Unit newUnit = new Unit()
                    {
                        Name = UserInputUnit.Name,
                        DateStart = UserInputUnit.DateStart,
                        DateEnd = UserInputUnit.DateEnd
                    };
                    DataProvider.AddUnitAsync(newUnit);
                    Units.Add(newUnit);
                }
                UserInputUnit = new Unit();
                RaisePropertyChanged(nameof(UserInput));
            });

            DeleteUnit = new RelayCommand<Unit>(unit =>
            {
                if (unit != null)
                {
                    DataProvider.DeleteUnit(unit);
                    Units.Remove(unit);
                }
            });

            EditUnit = new RelayCommand<Unit>(unit =>
            {
                UserInputUnit = unit;
                oldUnitName = unit.Name;
                InputUnitBox inputBox = new InputUnitBox();
                if (inputBox.ShowDialog() == true && !string.IsNullOrEmpty(UserInputUnit.Name))
                {
                    DataProvider.EditUnit(UserInputUnit, oldUnitName);
                }
            });
        }

        /// <summary>
        /// Инициализация коллекций
        /// </summary>
        private void CollectionsInit()
        {
            Units = new ObservableCollection<Unit>(DataProvider.GetUnits());
            Paths = new ObservableCollection<PCCOMM_Path>(DataProvider.GetPaths());
        }

        /// <summary>
        /// Добавить путь к Alfamos
        /// </summary>
        /// <param name="session"></param>
        private void AddAlfamos(DataProvider.SesionName session)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = $"{session.ToString()} | {session.ToString()}.ws;{session.ToString()}.exe";
            if (fileDialog.ShowDialog() == true)
            {
                for (int i = 0; i < Paths.Count; i++)
                {
                    if (Paths[i].SessionName.Equals(session.ToString()))
                    {
                        Paths[i].Path = fileDialog.FileName;
                        DataProvider.EditPath(Paths[i]);
                        return;
                    }
                }

                //No available Path
                DataProvider.AddPathAsync(fileDialog.FileName, session);
                Paths.Add(new PCCOMM_Path()
                {
                    Path = fileDialog.FileName,
                    SessionName = session.ToString()
                });
            }
        }

        #endregion


        #region Сгенерировать выписку

        private void GetReport()
        {
            Log = string.Empty;
            #region узнаем у пользователя путь к файлу
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Excel File (*.xlsx)|*.xlsx";
            if (fileDialog.ShowDialog() != true) return;
            #endregion
            AddToLog("Получена команда на работу.");
            AddToLog($"Обработка файла {fileDialog.FileName}.");
            #region подготовка к работе
            PCCOMM_Path alfamos2 = Paths.FirstOrDefault(i => i.SessionName == "Alfamos2");
            PCCOMM_Path alfamos4 = Paths.FirstOrDefault(i => i.SessionName == "Alfamos4");
            PCCOMM_Path pcscm = Paths.FirstOrDefault(i => i.SessionName == "pcscm");
            Process currentAlfamos;
            if (alfamos2 == null || alfamos4 == null)
            {
                MessageBox.Show("Не указан путь к alfamos2 или alfamos4. Пожалуйста, перейдите в настройки и настройте пути", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                if (alfamos2 == null)
                    AddToLog("Не указан путь к alfamos2");
                if (alfamos4 == null)
                    AddToLog("Не указан путь к alfamos4");
                if (pcscm == null)
                    AddToLog("Не указан путь к pcscm");
                return;
            }

            EquationWorker equation = new EquationWorker(alfamos2, alfamos4, pcscm);
            ExcelWorker excel = new ExcelWorker(new FileInfo(fileDialog.FileName));
            List<Account> accounts = new List<Account>();
            #endregion

            #region Получить все счета из файла
            AddToLog("Чтение счетов из исходного файла.");
            accounts = excel.ReadForAccounts().Select(i => new Account { Number = i }).ToList();
            #endregion
            AddToLog("Завершено.");
            AddToLog("Получение дополнительных сведений о счетах из боевого EQ.");
            #region Получить доп. информацию о счетах (дата открытия/закрытия)
            AddToLog("  Устанавливаю соединение.");
            char connectedChar = equation.OpenConnection(alfamos4, out currentAlfamos);
            AddToLog("  Ищем информацию.");
            equation.FillAccounts(accounts, connectedChar);
            AddToLog("  Сведения о пользователе.");
            equation.GetUserProfile(connectedChar);
            AddToLog("  Закрываю соединение.");
            currentAlfamos.CloseMainWindow();
            currentAlfamos.WaitForExit(5000);
            AddToLog("Завершено.");


            #endregion

            #region Генерировать выписку

            int l = 1;
            foreach (Account acc in accounts)
            {
                List<Operation> AccountOperations = new List<Operation>();

                AddToLog($"Счет {l++}/{accounts.Count}.");
                #region получение списка юнитов для каждого счета
                DateTime startDate = acc.OpenedDate;
                bool done = false;
                while (!done)
                {
                    done = true;
                    foreach (Unit unit in Units)
                    {
                        if (unit.DateStart <= startDate &&
                            unit.DateEnd >= startDate)
                        {
                            acc.Units.Add(unit);
                            startDate = unit.DateEnd.AddDays(1);
                            done = false;
                        }
                    }

                    if (acc.Units.Count < 1)
                    {
                        MessageBox.Show($"Для счета {acc.Number} отсутствует один или несколько юнитов. Не найдено вхождения для даты {startDate.ToString("dd.MM.yyyy")}", "Не укаан юнит", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
                #endregion

                #region выгрузка TZ
                //Для каждого юнита
                //Запускаем EQ
                //Проверяем наличие оборота за период
                //При наличии оборота - выгружаем TZ, при отсутствии закрываем EQ
                //Загружаем данные об операциях из файла xlsx

                foreach (Unit unit in acc.Units)
                {
                    AddToLog($" Выгрузка TZ - unit {unit.Name}.");
                    equation.OpenConnection(alfamos2, out currentAlfamos);
                    equation.EnterUnit(unit);
                    if (equation.CheckForTurnover(acc))//если по счету есть оборот
                    {
                        AccountOperations.AddRange(equation.GetOperations(acc, currentAlfamos, unit));
                    }
                }
                #endregion
                AddToLog($"Сохраняю в .xlsx. {Path.GetDirectoryName(fileDialog.FileName)}\\{ acc.Number}Res.xlsx");
                #region Сохранить Excel-файл
                excel.GenerateTz(AccountOperations, $@"{Path.GetDirectoryName(fileDialog.FileName)}\{acc.Number}Res.xlsx");
                #endregion
            }
            #endregion


            #endregion

            #region cleanup
            AddToLog("Формирование выписок завершено. Прибираемся...");
            foreach(Process process in Process.GetProcessesByName("pcscm"))
            {
                process.Kill();
            }

            foreach (Process process in Process.GetProcessesByName("pcsws"))
            {
                process.Kill();
            }
            #endregion
            AddToLog("Готов к работе.");

        }
        private void AddToLog(string message)
        {
            Log = $"{message}\r\n{Log}";
            RaisePropertyChanged("Log");
        }

        
    }
}