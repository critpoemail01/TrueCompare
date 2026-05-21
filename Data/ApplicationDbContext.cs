using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TrueCompare.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<SearchRequest> SearchRequests => Set<SearchRequest>();

    public DbSet<CreditPurchase> CreditPurchases => Set<CreditPurchase>();

    public DbSet<TargetPriceAlert> TargetPriceAlerts => Set<TargetPriceAlert>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(120);
            entity.Property(user => user.FreeSearchesUsed).HasDefaultValue(0);
            entity.Property(user => user.Credits).HasDefaultValue(0);
            entity.Property(user => user.HasUnlimitedSubscription).HasDefaultValue(false);
            entity.Property(user => user.SubscriptionPlanId).HasMaxLength(80);
            entity.Property(user => user.StripeSubscriptionId).HasMaxLength(200);
            entity.HasIndex(user => user.StripeSubscriptionId);
            entity.Property(user => user.CreatedUtc).HasDefaultValueSql("GETUTCDATE()");
        });

        builder.Entity<SearchRequest>(entity =>
        {
            entity.Property(search => search.Query).HasMaxLength(800).IsRequired();
            entity.Property(search => search.CreatedUtc).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(search => new { search.UserId, search.CreatedUtc });
            entity.HasOne(search => search.User)
                .WithMany()
                .HasForeignKey(search => search.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CreditPurchase>(entity =>
        {
            entity.Property(purchase => purchase.StripeSessionId).HasMaxLength(200).IsRequired();
            entity.Property(purchase => purchase.StripePaymentIntentId).HasMaxLength(200);
            entity.Property(purchase => purchase.StripeSubscriptionId).HasMaxLength(200);
            entity.Property(purchase => purchase.BillingKind)
                .HasMaxLength(40)
                .HasDefaultValue(CreditBillingKind.OneTime)
                .IsRequired();
            entity.Property(purchase => purchase.PlanId).HasMaxLength(80);
            entity.Property(purchase => purchase.BillingPeriod).HasMaxLength(20);
            entity.Property(purchase => purchase.Currency).HasMaxLength(3).IsRequired();
            entity.Property(purchase => purchase.Status).HasMaxLength(40).IsRequired();
            entity.Property(purchase => purchase.CreatedUtc).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(purchase => purchase.StripeSessionId).IsUnique();
            entity.HasOne(purchase => purchase.User)
                .WithMany()
                .HasForeignKey(purchase => purchase.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TargetPriceAlert>(entity =>
        {
            entity.Property(alert => alert.ProductSlug).HasMaxLength(120).IsRequired();
            entity.Property(alert => alert.ProductName).HasMaxLength(200).IsRequired();
            entity.Property(alert => alert.LastSeenSeller).HasMaxLength(160);
            entity.Property(alert => alert.ProductUrl).HasMaxLength(500);
            entity.Property(alert => alert.CreatedUtc).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(alert => new { alert.UserId, alert.IsActive, alert.EmailSent });
            entity.HasOne(alert => alert.User)
                .WithMany()
                .HasForeignKey(alert => alert.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
