using System;
using System.Collections.Generic;
using System.Linq;

namespace TZ_Robo.Model
{
    public class DataProvider : IDisposable
    {
        public enum SesionName
        {
            Alfamos2 = 0,
            Alfamos4 = 1
        }
        ConfiguresContext _dbContext;
        public DataProvider()
        {
            _dbContext = new ConfiguresContext();
        }

        public List<PCCOMM_Path> GetPaths() => _dbContext.PCCOMM_Paths.
            Where(i => i.User.ToLower().
            Equals(Environment.UserName.ToLower())).
            ToList();

        public List<Unit> GetUnits() => _dbContext.UnitsSet.ToList();

        public async void AddPathAsync(string PathStr, SesionName sessionName)
        {
            _dbContext.PCCOMM_Paths.Add(new PCCOMM_Path()
            {
                Path = PathStr,
                SessionName = sessionName.ToString(),
                User = Environment.UserName.ToLower()
            });
            await _dbContext.SaveChangesAsync();
        }

        public void EditPath(PCCOMM_Path path)
        {
            _dbContext.PCCOMM_Paths.FirstOrDefault(i => i.SessionName.Equals(path.SessionName)).Path = path.Path;
            _dbContext.SaveChanges();
        }

        public async void AddUnitAsync(Unit newUnit)
        {
            _dbContext.UnitsSet.Add(newUnit);
            await _dbContext.SaveChangesAsync();
        }

        public void DeleteUnit(Unit unit)
        {
            _dbContext.UnitsSet.Remove(_dbContext.UnitsSet.FirstOrDefault(i => i.Name == unit.Name));
            _dbContext.SaveChanges();
        }

        public void EditUnit(Unit unit, string oldUnitname)
        {
            Unit oldUnit = _dbContext.UnitsSet.FirstOrDefault(i => i.Name == oldUnitname);
            oldUnit.Name = unit.Name;
            oldUnit.DateStart = unit.DateStart;
            oldUnit.DateEnd = unit.DateEnd;
            _dbContext.SaveChanges();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
