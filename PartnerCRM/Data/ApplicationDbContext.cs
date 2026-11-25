
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using PartnerCRM.Models;

namespace PartnerCRM.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<ActivityAttachment> ActivityAttachments { get; set; }
        public DbSet<CrmTask> Tasks { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Bank> Banks { get; set; }
        public DbSet<CompanyType> CompanyTypes { get; set; }
        public DbSet<ContactEmployment> ContactEmployments { get; set; }
        public DbSet<ProductType> ProductTypes { get; set; }
        public DbSet<ProductionArea> ProductionAreas { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<MarketingCampaign> MarketingCampaigns { get; set; }
        public DbSet<MarketingRecipient> MarketingRecipients { get; set; }
        public DbSet<UserInvitation> UserInvitations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Organizační struktura firem
            modelBuilder.Entity<Company>()
                .HasOne(c => c.ParentCompany)
                .WithMany(c => c.Branches)
                .HasForeignKey(c => c.ParentCompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Evidence sloučení firem
            modelBuilder.Entity<Company>()
                .HasOne(c => c.MergedIntoCompany)
                .WithMany(c => c.MergedCompanies)
                .HasForeignKey(c => c.MergedIntoCompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Evidence sloučení kontaktů
            modelBuilder.Entity<Contact>()
                .HasOne(c => c.MergedIntoContact)
                .WithMany(c => c.MergedContacts)
                .HasForeignKey(c => c.MergedIntoContactId)
                .OnDelete(DeleteBehavior.Restrict);

            // Historie působení kontaktů ve firmách
            modelBuilder.Entity<ContactEmployment>()
                .HasOne(e => e.Contact)
                .WithMany(c => c.Employments)
                .HasForeignKey(e => e.ContactId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContactEmployment>()
                .HasOne(e => e.Company)
                .WithMany(c => c.ContactEmployments)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Nejvýše jeden aktivní hlavní kontakt pro každou firmu
            modelBuilder.Entity<ContactEmployment>()
                .HasIndex(e => e.CompanyId)
                .IsUnique()
                .HasFilter("\"IsPrimary\" = 1 AND \"IsCurrent\" = 1");

            // Hierarchie oblastí produkce
            modelBuilder.Entity<ProductionArea>()
                .HasOne(p => p.ParentProductionArea)
                .WithMany(p => p.SubAreas)
                .HasForeignKey(p => p.ParentProductionAreaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Hierarchie aktivit
            modelBuilder.Entity<Activity>()
                .HasOne(a => a.ParentActivity)
                .WithMany(a => a.ChildActivities)
                .HasForeignKey(a => a.ParentActivityId)
                .OnDelete(DeleteBehavior.Restrict);

            // Přílohy aktivit
            modelBuilder.Entity<ActivityAttachment>()
                .HasOne(a => a.Activity)
                .WithMany(a => a.Attachments)
                .HasForeignKey(a => a.ActivityId)
                .OnDelete(DeleteBehavior.Cascade);

            // Vazba úkolu na aktivitu
            modelBuilder.Entity<CrmTask>()
                .HasOne(t => t.Activity)
                .WithMany()
                .HasForeignKey(t => t.ActivityId)
                .OnDelete(DeleteBehavior.SetNull);

            // Hierarchie úkolů
            modelBuilder.Entity<CrmTask>()
                .HasOne(t => t.ParentTask)
                .WithMany(t => t.ChildTasks)
                .HasForeignKey(t => t.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            // Vazba aktivity na související úkol
            modelBuilder.Entity<Activity>()
                .HasOne(a => a.Task)
                .WithMany(t => t.Activities)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unikátní zkratka typu zboží
            modelBuilder.Entity<ProductType>()
                .HasIndex(p => p.Code)
                .IsUnique();

            // Marketingová akce – e-mailová šablona
            modelBuilder.Entity<MarketingCampaign>()
                .HasOne(c => c.EmailTemplate)
                .WithMany()
                .HasForeignKey(c => c.EmailTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Příjemci marketingové akce
            modelBuilder.Entity<MarketingRecipient>()
                .HasOne(r => r.MarketingCampaign)
                .WithMany(c => c.Recipients)
                .HasForeignKey(r => r.MarketingCampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MarketingRecipient>()
                .HasOne(r => r.Company)
                .WithMany()
                .HasForeignKey(r => r.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MarketingRecipient>()
                .HasOne(r => r.Contact)
                .WithMany()
                .HasForeignKey(r => r.ContactId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MarketingRecipient>()
                .HasIndex(r => new { r.MarketingCampaignId, r.EmailAddress })
                .IsUnique();

            // Marketingová kampaň – související aktivity
            modelBuilder.Entity<Activity>()
                .HasOne(a => a.MarketingCampaign)
                .WithMany(c => c.Activities)
                .HasForeignKey(a => a.MarketingCampaignId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
