using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using TZ_Robo.Model;

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
        #endregion

        DataProvider DataProvider = new DataProvider();

        #region Commands
        public RelayCommand AddAlfamos2Command { get; set; }
        public RelayCommand AddAlfamos4Command { get; set; }
        public RelayCommand Upload { get; set; }
        public RelayCommand AddUnit { get; set; }
        public RelayCommand<Unit> DeleteUnit { get; set; }
        public RelayCommand<Unit> EditUnit { get; set; }
        
        #endregion

        public MainViewModel()
        {
            CollectionsInit();
            CommandsInit();

        }

        #region Initialization

        /// <summary>
        /// Инициализация всех команд
        /// </summary>
        private void CommandsInit()
        {
            AddAlfamos2Command = new RelayCommand(() => AddAlfamos(DataProvider.SesionName.Alfamos2));
            AddAlfamos4Command = new RelayCommand(() => AddAlfamos(DataProvider.SesionName.Alfamos4));

            Upload = new RelayCommand(() =>
            {

            });

            AddUnit = new RelayCommand(() =>
            {
                InputUnitBox inputBox = new InputUnitBox();
                if (inputBox.ShowDialog() == true && !string.IsNullOrEmpty(UserInputUnit.Name))
                {
                    DataProvider.AddUnitAsync(new Unit()
                    {
                        Name = UserInputUnit.Name,
                        DateStart = UserInputUnit.DateStart,
                        DateEnd = UserInputUnit.DateEnd
                    });
                }
                Units.Add(UserInputUnit);
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
            fileDialog.Filter = $"{session.ToString()}.ws | {session.ToString()}.ws";
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
    }
}