using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OrgTechRepair.Models;

namespace OrgTechRepair.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Part> Parts { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderPart> OrderParts { get; set; }
    public DbSet<PartSupplyRequest> PartSupplyRequests { get; set; }
    public DbSet<WorkType> WorkTypes { get; set; }
    public DbSet<Work> Works { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleItem> SaleItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure relationships
        builder.Entity<Employee>()
            .HasOne(e => e.Position)
            .WithMany(p => p.Employees)
            .HasForeignKey(e => e.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Product>()
            .HasOne(p => p.Supplier)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Part>()
            .HasOne(p => p.Supplier)
            .WithMany(s => s.Parts)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Order>()
            .HasOne(o => o.Client)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Order>()
            .HasOne(o => o.Employee)
            .WithMany()
            .HasForeignKey(o => o.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<OrderPart>()
            .HasOne(op => op.Order)
            .WithMany(o => o.OrderParts)
            .HasForeignKey(op => op.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<OrderPart>()
            .HasOne(op => op.Part)
            .WithMany(p => p.OrderParts)
            .HasForeignKey(op => op.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PartSupplyRequest>()
            .HasOne(r => r.Part)
            .WithMany()
            .HasForeignKey(r => r.PartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<PartSupplyRequest>()
            .HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Work>()
            .HasOne(w => w.Order)
            .WithMany(o => o.Works)
            .HasForeignKey(w => w.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Work>()
            .HasOne(w => w.WorkType)
            .WithMany(wt => wt.Works)
            .HasForeignKey(w => w.WorkTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Sale>()
            .HasOne(s => s.Client)
            .WithMany(c => c.Sales)
            .HasForeignKey(s => s.ClientId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<SaleItem>()
            .HasOne(si => si.Sale)
            .WithMany(s => s.SaleItems)
            .HasForeignKey(si => si.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SaleItem>()
            .HasOne(si => si.Product)
            .WithMany(p => p.SaleItems)
            .HasForeignKey(si => si.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Даты сотрудников и продаж храним как date (без времени).
        // Для заявок важно время создания/выполнения, поэтому OrderDate/CompletionDate — timestamp без timezone.
        builder.Entity<Employee>().Property(e => e.DateOfBirth).HasColumnType("date");
        builder.Entity<Employee>().Property(e => e.HireDate).HasColumnType("date");
        builder.Entity<Order>().Property(o => o.OrderDate).HasColumnType("timestamp without time zone");
        builder.Entity<Order>().Property(o => o.CompletionDate).HasColumnType("timestamp without time zone");
        builder.Entity<Sale>().Property(s => s.SaleDate).HasColumnType("date");
    }
}
