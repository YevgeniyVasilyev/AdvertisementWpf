
using AdvertisementWpf.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;

namespace AdvertisementWpf
{
    public static class CreateDbContext
    {
        public static App.AppDbContext CreateContext()
        {
            return new App.AppDbContext(MainWindow.Connectiondata.Connectionstring);
        }
    }
}
