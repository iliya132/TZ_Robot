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
            Log = "����� � ������";
        }

        #region Initialization

        /// <summary>
        /// ������������� ���� ������
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
        /// ������������� ���������
        /// </summary>
        private void CollectionsInit()
        {
            Units = new ObservableCollection<Unit>(DataProvider.GetUnits());
            Paths = new ObservableCollection<PCCOMM_Path>(DataProvider.GetPaths());
        }

        /// <summary>
        /// �������� ���� � Alfamos
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


        #region ������������� �������

        private void GetReport()
        {
            Log = string.Empty;
            #region ������ � ������������ ���� � �����
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Excel File (*.xlsx)|*.xlsx";
            if (fileDialog.ShowDialog() != true) return;
            #endregion
            AddToLog("�������� ������� �� ������.");
            AddToLog($"��������� ����� {fileDialog.FileName}.");
            #region ���������� � ������
            PCCOMM_Path alfamos2 = Paths.FirstOrDefault(i => i.SessionName == "Alfamos2");
            PCCOMM_Path alfamos4 = Paths.FirstOrDefault(i => i.SessionName == "Alfamos4");
            PCCOMM_Path pcscm = Paths.FirstOrDefault(i => i.SessionName == "pcscm");
            Process currentAlfamos;
            if (alfamos2 == null || alfamos4 == null)
            {
                MessageBox.Show("�� ������ ���� � alfamos2 ��� alfamos4. ����������, ��������� � ��������� � ��������� ����", "������", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                if (alfamos2 == null)
                    AddToLog("�� ������ ���� � alfamos2");
                if (alfamos4 == null)
                    AddToLog("�� ������ ���� � alfamos4");
                if (pcscm == null)
                    AddToLog("�� ������ ���� � pcscm");
                return;
            }

            EquationWorker equation = new EquationWorker(alfamos2, alfamos4, pcscm);
            ExcelWorker excel = new ExcelWorker(new FileInfo(fileDialog.FileName));
            List<Account> accounts = new List<Account>();
            #endregion

            #region �������� ��� ����� �� �����
            AddToLog("������ ������ �� ��������� �����.");
            accounts = excel.ReadForAccounts().Select(i => new Account { Number = i }).ToList();
            #endregion
            AddToLog("���������.");
            AddToLog("��������� �������������� �������� � ������ �� ������� EQ.");
            #region �������� ���. ���������� � ������ (���� ��������/��������)
            AddToLog("  ������������ ����������.");
            char connectedChar = equation.OpenConnection(alfamos4, out currentAlfamos);
            AddToLog("  ���� ����������.");
            equation.FillAccounts(accounts, connectedChar);
            AddToLog("  �������� � ������������.");
            equation.GetUserProfile(connectedChar);
            AddToLog("  �������� ����������.");
            currentAlfamos.CloseMainWindow();
            currentAlfamos.WaitForExit(5000);
            AddToLog("���������.");


            #endregion

            #region ������������ �������

            int l = 1;
            foreach (Account acc in accounts)
            {
                List<Operation> AccountOperations = new List<Operation>();

                AddToLog($"���� {l++}/{accounts.Count}.");
                #region ��������� ������ ������ ��� ������� �����
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
                        MessageBox.Show($"��� ����� {acc.Number} ����������� ���� ��� ��������� ������. �� ������� ��������� ��� ���� {startDate.ToString("dd.MM.yyyy")}", "�� ����� ����", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
                #endregion

                #region �������� TZ
                //��� ������� �����
                //��������� EQ
                //��������� ������� ������� �� ������
                //��� ������� ������� - ��������� TZ, ��� ���������� ��������� EQ
                //��������� ������ �� ��������� �� ����� xlsx

                foreach (Unit unit in acc.Units)
                {
                    AddToLog($" �������� TZ - unit {unit.Name}.");
                    equation.OpenConnection(alfamos2, out currentAlfamos);
                    equation.EnterUnit(unit);
                    if (equation.CheckForTurnover(acc))//���� �� ����� ���� ������
                    {
                        AccountOperations.AddRange(equation.GetOperations(acc, currentAlfamos, unit));
                    }
                }
                #endregion
                AddToLog($"�������� � .xlsx. {Path.GetDirectoryName(fileDialog.FileName)}\\{ acc.Number}Res.xlsx");
                #region ��������� Excel-����
                excel.GenerateTz(AccountOperations, $@"{Path.GetDirectoryName(fileDialog.FileName)}\{acc.Number}Res.xlsx");
                #endregion
            }
            #endregion


            #endregion

            #region cleanup
            AddToLog("������������ ������� ���������. �����������...");
            foreach(Process process in Process.GetProcessesByName("pcscm"))
            {
                process.Kill();
            }

            foreach (Process process in Process.GetProcessesByName("pcsws"))
            {
                process.Kill();
            }
            #endregion
            AddToLog("����� � ������.");

        }
        private void AddToLog(string message)
        {
            Log = $"{message}\r\n{Log}";
            RaisePropertyChanged("Log");
        }

        
    }
}