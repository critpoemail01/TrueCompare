using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TrueCompare.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<SearchRequest> SearchRequests => Set<SearchRequest>();

    public DbSet<SearchResultSnapshot> SearchResultSnapshots => Set<SearchResultSnapshot>();

    public DbSet<CreditPurchase> CreditPurchases => Set<CreditPurchase>();

    public DbSet<StripeProcessedEvent> StripeProcessedEvents => Set<StripeProcessedEvent>();

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
            entity.Property(user => user.StripeCustomerId).HasMaxLength(200);
            entity.HasIndex(user => user.StripeCustomerId);
            entity.Property(user => user.StripeSubscriptionId).HasMaxLength(200);
            entity.HasIndex(user => user.StripeSubscriptionId);
            entity.Property(user => user.CreatedUtc).HasDefaultValueSql("GETUTCDATE()");
        });

        builder.Entity<SearchRequest>(entity =>
        {
            entity.Property(search => search.Query).HasMaxLength(800).IsRequired();
            entity.Property(search => search.UsedFreeSearch).HasDefaultValue(false);
            entity.Property(search => search.Status)
                .HasMaxLength(40)
                .HasDefaultValue(SearchRequestStatus.Committed)
                .IsRequired();
            entity.Property(search => search.IdempotencyKey).HasMaxLength(160);
            entity.Property(search => search.FailureReason).HasMaxLength(500);
            entity.Property(search => search.ResultCount).HasDefaultValue(0);
            entity.Property(search => search.CreatedUtc).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(search => new { search.UserId, search.CreatedUtc });
            entity.HasIndex(search => new { search.UserId, search.IdempotencyKey })
                .IsUnique()
                .HasFilter("[IdempotencyKey] IS NOT NULL");
            entity.HasIndex(search => new { search.UserId, search.Status, search.CreatedUtc });
            entity.HasOne(search => search.User)
                .WithMany()
                .HasForeignKey(search => search.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        builder.Entity<SearchResultSnapshot>(entity =>
        {
            entity.Property(snapshot => snapshot.UserId).HasMaxLength(450).IsRequired();
            entity.Property(snapshot => snapshot.Query).HasMaxLength(800).IsRequired();
            entity.Property(snapshot => snapshot.IdempotencyKey).HasMaxLength(160).IsRequired();
            entity.Property(snapshot => snapshot.ProductsJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(snapshot => snapshot.OffersJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(snapshot => snapshot.ProductOfferSummariesJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(snapshot => snapshot.AiSuggestionsJson).HasColumnType("nvarchar(max)");
            entity.Property(snapshot => snapshot.ResultCount).HasDefaultValue(0);
            entity.Property(snapshot => snapshot.CreatedUtc).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(snapshot => snapshot.ExpiresUtc).HasColumnType("datetime2");
            entity.HasIndex(snapshot => new { snapshot.UserId, snapshot.IdempotencyKey }).IsUnique();
            entity.HasIndex(snapshot => new { snapshot.UserId, snapshot.Query, snapshot.CreatedUtc });
            entity.HasIndex(snapshot => snapshot.SearchRequestId);
            entity.HasIndex(snapshot => snapshot.ExpiresUtc);
            entity.HasOne(snapshot => snapshot.User)
                .WithMany()
                .HasForeignKey(snapshot => snapshot.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(snapshot => snapshot.SearchRequest)
                .WithMany()
                .HasForeignKey(snapshot => snapshot.SearchRequestId)
                .OnDelete(DeleteBehavior.NoAction);
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

        builder.Entity<StripeProcessedEvent>(entity =>
        {
            entity.Property(stripeEvent => stripeEvent.StripeEventId).HasMaxLength(200).IsRequired();
            entity.Property(stripeEvent => stripeEvent.EventType).HasMaxLength(120).IsRequired();
            entity.Property(stripeEvent => stripeEvent.Status).HasMaxLength(40).IsRequired();
            entity.Property(stripeEvent => stripeEvent.Error).HasMaxLength(500);
            entity.Property(stripeEvent => stripeEvent.CreatedUtc).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(stripeEvent => stripeEvent.ProcessedUtc).HasColumnType("datetime2");
            entity.HasIndex(stripeEvent => stripeEvent.StripeEventId).IsUnique();
            entity.HasIndex(stripeEvent => new { stripeEvent.Status, stripeEvent.CreatedUtc });
        });

        builder.Entity<TargetPriceAlert>(entity =>
        {
            entity.Property(alert => alert.ProductSlug).HasMaxLength(120).IsRequired();
            entity.Property(alert => alert.ProductName).HasMaxLength(200).IsRequired();
            entity.Property(alert => alert.LastSeenSeller).HasMaxLength(160);
            entity.Property(alert => alert.ProductUrl).HasMaxLength(500);
            entity.Property(alert => alert.LastValidationState)
                .HasMaxLength(40)
                .HasDefaultValue("CatalogKnown")
                .IsRequired();
            entity.Property(alert => alert.LastValidatedUtc).HasColumnType("datetime2");
            entity.Property(alert => alert.EmailFailureCount).HasDefaultValue(0);
            entity.Property(alert => alert.LastEmailError).HasMaxLength(500);
            entity.Property(alert => alert.NextEmailRetryUtc).HasColumnType("datetime2");
            entity.Property(alert => alert.EmailSuppressedUtc).HasColumnType("datetime2");
            entity.Property(alert => alert.CreatedUtc).HasDefaultValueSql("GETUTCDATE()");
            entity.HasIndex(alert => new { alert.UserId, alert.IsActive, alert.EmailSent });
            entity.HasOne(alert => alert.User)
                .WithMany()
                .HasForeignKey(alert => alert.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
