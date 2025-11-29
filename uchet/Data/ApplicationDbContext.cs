using Microsoft.EntityFrameworkCore;
using uchet.Models;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace uchet.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<PropertyType> PropertyTypes { get; set; }
        public DbSet<PropertyFile> PropertyFiles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Настройка уникального индекса для InventoryNumber
            modelBuilder.Entity<Property>()
                .HasIndex(p => p.InventoryNumber)
                .IsUnique();

            // Настройка связи между User и Role
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId);
            
            // Настройка связи между User и Location
            modelBuilder.Entity<User>()
                .HasOne(u => u.Location)
                .WithMany()
                .HasForeignKey(u => u.LocationId)
                .OnDelete(DeleteBehavior.SetNull);

            // Настройка связи между Property и Location
            modelBuilder.Entity<Property>()
                .HasOne(p => p.Location)
                .WithMany(l => l.Properties)
                .HasForeignKey(p => p.LocationId);

            // Настройка связи между Property и PropertyType
            modelBuilder.Entity<Property>()
                .HasOne(p => p.PropertyType)
                .WithMany(pt => pt.Properties)
                .HasForeignKey(p => p.PropertyTypeId);

            // Настройка связи между Property и User (назначение)
            modelBuilder.Entity<Property>()
                .HasOne(p => p.AssignedUser)
                .WithMany()
                .HasForeignKey(p => p.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Настройка связи между Property и PropertyFile
            modelBuilder.Entity<PropertyFile>()
                .HasOne(pf => pf.Property)
                .WithMany(p => p.PropertyFiles)
                .HasForeignKey(pf => pf.PropertyId);
                
            // Настройка связи между Inventory и InventoryItem
            modelBuilder.Entity<InventoryItem>()
                .HasOne(ii => ii.Inventory)
                .WithMany(i => i.InventoryItems)
                .HasForeignKey(ii => ii.InventoryId);
                
            // Настройка связи между InventoryItem и Property
            modelBuilder.Entity<InventoryItem>()
                .HasOne(ii => ii.Property)
                .WithMany()
                .HasForeignKey(ii => ii.PropertyId);

            // Добавляем тестовые данные
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "Manager" },
                new Role { Id = 3, Name = "User" }
            );

            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Name = "Administrator", Email = "admin@example.com", Password = "admin", RoleId = 1 }
            );
            
            // Добавляем тестовые разрешения для ролей
            modelBuilder.Entity<RolePermission>().HasData(
                new RolePermission { Id = 1, RoleId = 1, ControllerName = "Home", ActionName = "Index" },
                new RolePermission { Id = 2, RoleId = 1, ControllerName = "Home", ActionName = "Privacy" },
                new RolePermission { Id = 3, RoleId = 1, ControllerName = "Home", ActionName = "Contact" },
                new RolePermission { Id = 4, RoleId = 1, ControllerName = "Property", ActionName = "Index" },
                new RolePermission { Id = 5, RoleId = 1, ControllerName = "Property", ActionName = "Details" },
                new RolePermission { Id = 6, RoleId = 1, ControllerName = "Property", ActionName = "Create" },
                new RolePermission { Id = 7, RoleId = 1, ControllerName = "Property", ActionName = "Edit" },
                new RolePermission { Id = 8, RoleId = 1, ControllerName = "Property", ActionName = "Delete" },
                new RolePermission { Id = 9, RoleId = 1, ControllerName = "Property", ActionName = "Import" },
                new RolePermission { Id = 10, RoleId = 1, ControllerName = "Admin", ActionName = "Index" },
                new RolePermission { Id = 11, RoleId = 2, ControllerName = "Home", ActionName = "Index" },
                new RolePermission { Id = 12, RoleId = 2, ControllerName = "Home", ActionName = "Privacy" },
                new RolePermission { Id = 13, RoleId = 2, ControllerName = "Property", ActionName = "Index" },
                new RolePermission { Id = 14, RoleId = 2, ControllerName = "Property", ActionName = "Details" },
                new RolePermission { Id = 15, RoleId = 2, ControllerName = "Property", ActionName = "Create" },
                new RolePermission { Id = 16, RoleId = 2, ControllerName = "Property", ActionName = "Edit" },
                new RolePermission { Id = 17, RoleId = 3, ControllerName = "Home", ActionName = "Index" },
                new RolePermission { Id = 18, RoleId = 3, ControllerName = "Home", ActionName = "Privacy" },
                new RolePermission { Id = 19, RoleId = 3, ControllerName = "Property", ActionName = "Index" },
                new RolePermission { Id = 20, RoleId = 3, ControllerName = "Property", ActionName = "Details" }
            );
            
            // Добавляем тестовые типы имущества
            modelBuilder.Entity<PropertyType>().HasData(
                new PropertyType { Id = 1, Name = "Электроника", Description = "Электронные устройства" },
                new PropertyType { Id = 2, Name = "Мебель", Description = "Офисная мебель" },
                new PropertyType { Id = 3, Name = "Транспорт", Description = "Транспортные средства" },
                new PropertyType { Id = 4, Name = "Расходники", Description = "Расходные материалы" }
            );
            
            // Добавляем тестовые локации
            modelBuilder.Entity<Location>().HasData(
                new Location { Id = 1, Name = "Офис 101", Description = "Главный офис" },
                new Location { Id = 2, Name = "Склад A", Description = "Основной склад" },
                new Location { Id = 3, Name = "Конференц-зал", Description = "Зал для встреч" }
            );
        }
    }
}