
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Models;

namespace PartnerCRM.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }

        public DbSet<Contact> Contacts { get; set; }

        public DbSet<Activity> Activities { get; set; }

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
            // Hierarchie oblastí produkce
            modelBuilder.Entity<ProductionArea>()
                .HasOne(p => p.ParentProductionArea)
                .WithMany(p => p.SubAreas)
                .HasForeignKey(p => p.ParentProductionAreaId)
                .OnDelete(DeleteBehavior.Restrict);

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
        }
    }
}
