
using AdvertisementWpf.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;

namespace AdvertisementWpf
{
    internal interface ISave
    {
        void SaveContext(ref App.AppDbContext _context_, bool IsMutedMode = false)
        {
            try
            {
                _ = _context_.SaveChanges();
                if (!IsMutedMode)
                {
                    _ = MessageBox.Show("   Сохранено успешно!   ", "Сохранение данных");
                }
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка сохранения данных", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MainWindow.statusBar.ClearStatus();
            }
        }
    }

    internal interface ILoad
    {
        void LoadContext(long nOrderID);
    }

    internal interface IAdd
    {
        void AddToContext<T>(ref App.AppDbContext _appDbContext, T _object) where T : class
        {
            _ = _appDbContext.Set<T>().Add(_object);
        }
        void AddReferenceToContext(ref App.AppDbContext _appDbContext, object _parentObject, string _referenceObject)
        {
             _appDbContext.Entry(_parentObject).Reference(_referenceObject).Load();
        }
        T AddSingleToContext<T>(ref App.AppDbContext _appDbContext, T _parentObject, Func<T, bool> expression) where T : class
        {
            return _appDbContext.Set<T>().Single(expression);
        }
        void AddCollectionToContext(ref App.AppDbContext _appDbContext, object _parentObject, string _collectionObject)
        {
            _appDbContext.Entry(_parentObject).Collection(_collectionObject).Load();
        }
    }

    internal interface IDelete
    {
        void DeleteFromContext(object _object);
    }

    public static class CreateDbContext
    {
        public static App.AppDbContext CreateContext()
        {
            return new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
        }
    }

}
