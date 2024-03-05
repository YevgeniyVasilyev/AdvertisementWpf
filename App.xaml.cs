using System;
using System.Linq;
using System.Windows;
using AdvertisementWpf.Models;
using Microsoft.EntityFrameworkCore;

namespace AdvertisementWpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public class AppDbContext : DbContext
        {
            private readonly string _connectionString;

            public DbSet<Bank> Banks { get; set; }
            public DbSet<Client> Clients { get; set; }
            public DbSet<Contractor> Contractors { get; set; }
            public DbSet<Locality> Localities { get; set; }
            public DbSet<Order> Orders { get; set; }
            public DbSet<ParameterInProductType> ParameterInProductTypes { get; set; }
            public DbSet<Product> Products { get; set; }
            public DbSet<ProductCost> ProductCosts { get; set; }
            public DbSet<ProductType> ProductTypes { get; set; }
            public DbSet<Role> Roles { get; set; }
            public DbSet<TypeOfActivity> TypeOfActivitys { get; set; }
            public DbSet<TypeOfActivityInProduct> TypeOfActivityInProducts { get; set; }
            public DbSet<Unit> Units { get; set; }
            public DbSet<User> Users { get; set; }
            public DbSet<CategoryOfProduct> CategoryOfProducts { get; set; }
            public DbSet<Setting> Setting { get; set; }
            public DbSet<IAccessMatrix> IAccessMatrix { get; set; }
            public DbSet<Referencebook> Referencebook { get; set; }
            public DbSet<ReferencebookParameter> ReferencebookParameter { get; set; }
            public DbSet<ReferencebookApplicability> ReferencebookApplicability { get; set; }
            public DbSet<Payment> Payments { get; set; }
            public DbSet<Account> Accounts { get; set; }
            public DbSet<Act> Acts { get; set; }
            public DbSet<Report> Reports { get; set; }
            public DbSet<ProductionArea> ProductionAreas { get; set; }
            public DbSet<TypeOfActivityInProdArea> TypeOfActivityInProdAreas { get; set; }
            public DbSet<Operation> Operations { get; set; }
            public DbSet<TypeOfActivityInOperation> TypeOfActivityInOperations { get; set; }
            public DbSet<ParameterInOperation> ParameterInOperations { get; set; }
            public DbSet<OperationInWork> OperationInWorks { get; set; }
            public DbSet<TechCard> TechCards { get; set; }
            public DbSet<WorkInTechCard> WorkInTechCards { get; set; }

            public AppDbContext(string cs)
            {
                _connectionString = cs;
            }

            //public static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            //{
            //    builder.AddFilter((category, level) => category == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information).AddConsole();
            //});

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                _ = optionsBuilder
                    .UseLazyLoadingProxies(false)
                    .UseSqlServer(_connectionString, o => o.MaxBatchSize(1));
                //_ = optionsBuilder.UseSqlServer(_connectionString);
                //_ = optionsBuilder.LogTo(Console.WriteLine);
                //_ = optionsBuilder.UseLoggerFactory(loggerFactory);
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                _ = modelBuilder.Entity<Order>(o =>
                {
                    _ = o.Property(o => o.DateAdmission).HasColumnType("datetime");
                    _ = o.Property(o => o.DateCompletion).HasColumnType("datetime");
                    _ = o.Property(o => o.ID).ValueGeneratedOnAdd();
                    _ = o.HasKey("ID");
                }) ;
                _ = modelBuilder.Entity<Contractor>(c =>
                {
                    _ = c.Property(c => c.DirectorAttorneyDate).HasColumnType("datetime");
                    _ = c.Property(c => c.ChiefAccountantAttorneyDate).HasColumnType("datetime");
                });
                _ = modelBuilder.Entity<Payment>(p =>
                {
                    _ = p.Property(p => p.PaymentDate).HasColumnType("datetime");
                    _ = p.HasOne("Order");
                    _ = p.HasOne("Account");
                });
                _ = modelBuilder.Entity<Account>(a =>
                {
                    _ = a.Property(a => a.AccountDate).HasColumnType("datetime");
                    _ = a.Property(a => a.PayBeforeDate).HasColumnType("datetime");
                    _ = a.HasKey("ID");
                    _ = a.HasMany("Payments");
                });
                _ = modelBuilder.Entity<Act>(a =>
                {
                    _ = a.Property(a => a.ActDate).HasColumnType("datetime");
                });
                _ = modelBuilder.Entity<Product>(p =>
                {
                    _ = p.Property(p => p.DateDeliveryPlan).HasColumnType("datetime");
                    _ = p.Property(p => p.DateProductionLayout).HasColumnType("datetime");
                    _ = p.Property(p => p.DateApproval).HasColumnType("datetime");
                    _ = p.Property(p => p.DateManufacture).HasColumnType("datetime");
                    _ = p.Property(p => p.DateShipment).HasColumnType("datetime");
                    _ = p.Property(p => p.DateTransferApproval).HasColumnType("datetime");
                    _ = p.Property(p => p.DateTransferDesigner).HasColumnType("datetime");
                    _ = p.Property(p => p.DateTransferProduction).HasColumnType("datetime");
                    _ = p.Property(p => p.ID).ValueGeneratedOnAdd();
                    _ = p.HasKey("ID");
                    _ = p.HasIndex("OrderID");
                });
                _ = modelBuilder.Entity<ProductCost>(pc =>
                {
                    _ = pc.Property(pc => pc.ID).ValueGeneratedOnAdd();
                    _ = pc.HasKey("ID");
                    _ = pc.HasIndex("ProductID");
                });
                _ = modelBuilder.Entity<WorkInTechCard>(entity =>
                {
                    _ = entity.HasIndex(e => e.TechCardID, "IX_WorkInTechCards");
                    _ = entity.Property(e => e.DateFactCompletion).HasColumnType("datetime");
                    _ = entity.Property(e => e.DatePlanCompletion).HasColumnType("datetime");
                });
                _ = modelBuilder.Entity<Order>()
                    .HasOne("Manager")
                    .WithMany("OrderManagers");
                _ = modelBuilder.Entity<Order>()
                    .HasOne("OrderEntered")
                    .WithMany("OrderOrderEntereds");
                _ = modelBuilder.Entity<Client>()
                    .HasOne("Bank")
                    .WithMany("Clients");
                _ = modelBuilder.Entity<Client>()
                    .HasOne("ConsigneeBank")
                    .WithMany("ConsigneeClients");
                _ = modelBuilder.Entity<Order>().Navigation(o => o.OrderEntered).AutoInclude();
                _ = modelBuilder.Entity<Order>().Navigation(o => o.Manager).AutoInclude();
                _ = modelBuilder.Entity<Order>().Navigation(o => o.Client).AutoInclude();
                _ = modelBuilder.Entity<Product>().Navigation(p => p.Designer).AutoInclude();
            }

            public void SaveContext(bool IsMutedMode = false)
            {
                try
                {
                    _ = SaveChanges();
                    if (!IsMutedMode)
                    {
                        _ = MessageBox.Show("   Сохранено успешно!   ", "Сохранение данных");
                    }
                }
                catch (Exception ex)
                {
                    _ = MessageBox.Show(ex.Message + "\n" + ex?.InnerException?.Message, "Ошибка сохранения данных", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            public void AddToContext<T>(T _object) where T : class
            {
                _ = Set<T>().Add(_object);
                Entry(_object).State = EntityState.Added;
            }
            public void AddReferenceToContext(object _parentObject, string _referenceObject)
            {
                Entry(_parentObject).Reference(_referenceObject).Load();
            }
            public T AddSingleToContext<T>(T _parentObject, Func<T, bool> expression) where T : class
            {
                return Set<T>().SingleOrDefault(expression);
            }
            public void AddCollectionToContext(object _parentObject, string _collectionObject)
            {
                Entry(_parentObject).Collection(_collectionObject).Load();
            }
            public void DeleteFromContext<T>(T _object) where T : class
            {
                _ = Set<T>().Remove(_object);
            }
        }
    }
}
