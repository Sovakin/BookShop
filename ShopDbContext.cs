using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace BookShop;

public partial class ShopDbContext : DbContext
{
    private static ShopDbContext instance;

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Manufacturer> Manufacturers { get; set; }
    public DbSet<ProductPhoto> ProductPhotos { get; set; }
    public DbSet<Cart> Cart { get; set; }
    public DbSet<PurchaseHistory> PurchaseHistories { get; set; }

    public int activeUserID { private set; get; }

    public ShopDbContext()
    {
    }

    public ShopDbContext(DbContextOptions<ShopDbContext> options)
        : base(options)
    {
    }

    public static ShopDbContext Instance
    {
        get
        {
            if (instance == null)
                instance = new ShopDbContext();
            return instance;
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=ShopDB;Username=postgres;Password=123;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    public List<User> GetUsers() => Users.ToList();

    public List<Product> GetProducts() => Products.ToList();

    public List<Category> GetCategories() => Categories.ToList();

    public List<Manufacturer> GetManufacturers() => Manufacturers.ToList();

    public List<ProductPhoto> GetProductPhotos() => ProductPhotos.ToList();
    public List<Cart> GetCarts() => Cart.ToList();

    public void SetActiveUser(int id)
        => activeUserID = id;

    public bool CanGetAccess(AccessLevels reqLevel)
        => Users.Where(x => x.ClientID == activeUserID).First().Access >= (int)reqLevel;

}