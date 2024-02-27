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

            public virtual DbSet<NoTable> NoTable { get; }
            public virtual DbSet<Bank> Banks { get; set; }
            public virtual DbSet<Client> Clients { get; set; }
            public virtual DbSet<Contractor> Contractors { get; set; }
            public virtual DbSet<Locality> Localities { get; set; }
            public virtual DbSet<Order> Orders { get; set; }
            public virtual DbSet<ParameterInProductType> ParameterInProductTypes { get; set; }
            public virtual DbSet<Product> Products { get; set; }
            public virtual DbSet<ProductCost> ProductCosts { get; set; }
            public virtual DbSet<ProductType> ProductTypes { get; set; }
            public virtual DbSet<Role> Roles { get; set; }
            public virtual DbSet<TypeOfActivity> TypeOfActivitys { get; set; }
            public virtual DbSet<TypeOfActivityInProduct> TypeOfActivityInProducts { get; set; }
            public virtual DbSet<Unit> Units { get; set; }
            public virtual DbSet<User> Users { get; set; }
            public virtual DbSet<CategoryOfProduct> CategoryOfProducts { get; set; }
            public virtual DbSet<Setting> Setting { get; set; }
            public virtual DbSet<IAccessMatrix> IAccessMatrix { get; set; }
            public virtual DbSet<Referencebook> Referencebook { get; set; }
            public virtual DbSet<ReferencebookParameter> ReferencebookParameter { get; set; }
            public virtual DbSet<ReferencebookApplicability> ReferencebookApplicability { get; set; }
            public virtual DbSet<Payment> Payments { get; set; }
            public virtual DbSet<Account> Accounts { get; set; }
            public virtual DbSet<Act> Acts { get; set; }
            public virtual DbSet<Report> Reports { get; set; }
            public virtual DbSet<ProductionArea> ProductionAreas { get; set; }
            public virtual DbSet<TypeOfActivityInProdArea> TypeOfActivityInProdAreas { get; set; }
            public virtual DbSet<Operation> Operations { get; set; }
            public virtual DbSet<TypeOfActivityInOperation> TypeOfActivityInOperations { get; set; }
            public virtual DbSet<ParameterInOperation> ParameterInOperations { get; set; }
            public virtual DbSet<OperationInWork> OperationInWorks { get; set; }
            public virtual DbSet<TechCard> TechCards { get; set; }
            public virtual DbSet<WorkInTechCard> WorkInTechCards { get; set; }

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
                _ = modelBuilder.Entity<NoTable>(pc =>
                {
                    pc.HasNoKey();
                    //pc.ToView("");
                });
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
        }
    }
}
