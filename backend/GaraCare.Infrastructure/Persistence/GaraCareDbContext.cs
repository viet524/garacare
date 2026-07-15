using GaraCare.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GaraCare.Infrastructure.Persistence;

public class GaraCareDbContext : DbContext
{
    public GaraCareDbContext(DbContextOptions<GaraCareDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<WorkOrderStatusHistory> WorkOrderStatusHistories => Set<WorkOrderStatusHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GaraCareDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
