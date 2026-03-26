using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechUnited_AiStudio.Models;

namespace TechUnited_AiStudio.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    // 1. Table for AI Chat History
    public DbSet<ChatMessage> ChatMessages { get; set; }

    // 2. Table for RAG Knowledge Base (Document Chunks)
    public DbSet<KnowledgeChunk> KnowledgeChunks { get; set; }

    // 3. Table for Real-Time Registered User Chat (Added)
    public DbSet<PrivateMessage> PrivateMessages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Fixes the .NET 9 "PendingModelChangesWarning" error during migrations
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // --- SEED DATA CONFIGURATION ---
        const string ADMIN_ID = "a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4c5d";
        const string ROLE_ID = "bd76383a-449e-4a6c-9a4c-d965709d843c";

        builder.Entity<IdentityRole>().HasData(new IdentityRole
        {
            Id = ROLE_ID,
            Name = "Admin",
            NormalizedName = "ADMIN"
        });

        var adminUser = new ApplicationUser
        {
            Id = ADMIN_ID,
            UserName = "admin@techunited.net",
            NormalizedUserName = "ADMIN@TECHUNITED.NET",
            Email = "admin@techunited.net",
            NormalizedEmail = "ADMIN@TECHUNITED.NET",
            EmailConfirmed = true,
            SecurityStamp = "TECHUNITED_SECURE_STAMP_2026",
            ConcurrencyStamp = "TECHUNITED_CONCURRENCY_STAMP_2026"
        };

        adminUser.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(adminUser, "Se@ttle1603!");
        builder.Entity<ApplicationUser>().HasData(adminUser);

        builder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
        {
            RoleId = ROLE_ID,
            UserId = ADMIN_ID
        });

        // --- RELATIONSHIPS FOR PRIVATE MESSAGES ---

        // Configures Sender Relationship
        builder.Entity<PrivateMessage>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict); // Prevents accidental cascading deletes

        // Configures Receiver Relationship
        builder.Entity<PrivateMessage>()
            .HasOne(m => m.Receiver)
            .WithMany()
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- INDEXES FOR PERFORMANCE ---

        // AI Chat Indexes
        builder.Entity<ChatMessage>().HasIndex(m => m.UserId);
        builder.Entity<ChatMessage>().HasIndex(m => m.ConversationId);

        // RAG Knowledge Indexes
        builder.Entity<KnowledgeChunk>().HasIndex(k => k.UserId);
        builder.Entity<KnowledgeChunk>().HasIndex(k => k.FileName);

        // User Chat Indexes (Added for fast message retrieval)
        builder.Entity<PrivateMessage>().HasIndex(m => m.SenderId);
        builder.Entity<PrivateMessage>().HasIndex(m => m.ReceiverId);
    }
}