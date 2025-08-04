using DataManagementApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Internship> Internships { get; set; }
        public DbSet<Partner> Partners { get; set; }
        public DbSet<AcademicYear> AcademicYears { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Thesis> Theses { get; set; }
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<InternshipPeriod> InternshipPeriods { get; set; }
        public DbSet<ThesisPeriod> ThesisPeriods { get; set; }
        public DbSet<Business> Business { get; set; }
        public DbSet<ThesisPeriodBusiness> ThesisPeriodBusiness { get; set; }
        public DbSet<PartnerBusiness> PartnerBusiness { get; set; }

        // --- Models cho User, Role, Permission ---
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<RoleMenu> RoleMenus { get; set; }
        public DbSet<SystemSettings> SystemSettings { get; set; }
        
        // --- Matrix-based Permission System ---
        public DbSet<RoleModulePermission> RoleModulePermissions { get; set; }
        
        // --- Permission Audit System ---
        public DbSet<PermissionAuditLog> PermissionAuditLogs { get; set; }
        // -----------------------------------------

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình quan hệ cha-con cho Department
            modelBuilder.Entity<Department>()
                .HasOne(d => d.ParentDepartment)
                .WithMany(d => d.ChildDepartments)
                .HasForeignKey(d => d.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Cấu hình cho User, Role, Permission, Menu ---

            // User: Thiết lập unique index cho KeycloakUserId và Email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.KeycloakUserId)
                .IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Menu: Cấu hình quan hệ cha-con
            modelBuilder.Entity<Menu>()
                .HasOne(m => m.Parent)
                .WithMany(m => m.ChildMenus)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // UserRole: Khóa chính zusammengesetzt
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // RolePermission: Khóa chính zusammengesetzt
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);

            // RoleMenu: Khóa chính zusammengesetzt
            modelBuilder.Entity<RoleMenu>()
                .HasKey(rm => new { rm.RoleId, rm.MenuId });
            modelBuilder.Entity<RoleMenu>()
                .HasOne(rm => rm.Role)
                .WithMany(r => r.RoleMenus)
                .HasForeignKey(rm => rm.RoleId);
            modelBuilder.Entity<RoleMenu>()
                .HasOne(rm => rm.Menu)
                .WithMany(m => m.RoleMenus)
                .HasForeignKey(rm => rm.MenuId);

            // RoleModulePermission: New matrix-based permission system
            modelBuilder.Entity<RoleModulePermission>()
                .HasOne(rmp => rmp.Role)
                .WithMany(r => r.RoleModulePermissions)
                .HasForeignKey(rmp => rmp.RoleId);
            
            // Unique constraint: One permission record per role per module
            modelBuilder.Entity<RoleModulePermission>()
                .HasIndex(rmp => new { rmp.RoleId, rmp.ModuleName })
                .IsUnique();

            // Cấu hình cho InternshipPeriod và ThesisPeriod
            modelBuilder.Entity<InternshipPeriod>()
                .HasOne(p => p.AcademicYear)
                .WithMany()
                .HasForeignKey(p => p.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InternshipPeriod>()
                .HasOne(p => p.Semester)
                .WithMany()
                .HasForeignKey(p => p.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ThesisPeriod>()
                .HasOne(p => p.AcademicYear)
                .WithMany()
                .HasForeignKey(p => p.AcademicYearId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<ThesisPeriod>()
                .HasOne(p => p.Semester)
                .WithMany()
                .HasForeignKey(p => p.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Cấu hình cho Internship và Thesis để tránh lỗi Multiple Cascade Paths ---
            modelBuilder.Entity<Internship>()
                .HasOne(i => i.InternshipPeriod)
                .WithMany(p => p.Internships) 
                .HasForeignKey(i => i.InternshipPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Thesis>()
                .HasOne(t => t.ThesisPeriod)
                .WithMany(p => p.Theses) 
                .HasForeignKey(t => t.ThesisPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Internship to use User instead of Student
            modelBuilder.Entity<Internship>()
                .HasOne(i => i.Student)
                .WithMany()
                .HasForeignKey(i => i.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Thesis to use Student
            modelBuilder.Entity<Thesis>()
                .HasOne(t => t.Student)
                .WithMany()
                .HasForeignKey(t => t.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Thesis to use Supervisor (Lecturer)
            modelBuilder.Entity<Thesis>()
                .HasOne(t => t.Supervisor)
                .WithMany()
                .HasForeignKey(t => t.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Thesis to use Examiner (Lecturer) - optional
            modelBuilder.Entity<Thesis>()
                .HasOne(t => t.Examiner)
                .WithMany()
                .HasForeignKey(t => t.ExaminerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Lecturer: Thiết lập quan hệ với Department
            modelBuilder.Entity<Lecturer>()
                .HasOne(l => l.Department)
                .WithMany()
                .HasForeignKey(l => l.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình N-N ThesisPeriod <-> Business
            modelBuilder.Entity<ThesisPeriodBusiness>()
                .HasKey(tpbf => new { tpbf.ThesisPeriodId, tpbf.BusinessId });
            modelBuilder.Entity<ThesisPeriodBusiness>()
                .HasOne(tpbf => tpbf.ThesisPeriod)
                .WithMany(tp => tp.ThesisPeriodBusiness)
                .HasForeignKey(tpbf => tpbf.ThesisPeriodId);
            modelBuilder.Entity<ThesisPeriodBusiness>()
                .HasOne(tpbf => tpbf.Business)
                .WithMany(bf => bf.ThesisPeriodBusiness)
                .HasForeignKey(tpbf => tpbf.BusinessId);

            // Cấu hình N-N Partner <-> Business
            modelBuilder.Entity<PartnerBusiness>()
                .HasKey(pbf => new { pbf.PartnerId, pbf.BusinessId });
            modelBuilder.Entity<PartnerBusiness>()
                .HasOne(pbf => pbf.Partner)
                .WithMany(p => p.PartnerBusiness)
                .HasForeignKey(pbf => pbf.PartnerId);
            modelBuilder.Entity<PartnerBusiness>()
                .HasOne(pbf => pbf.Business)
                .WithMany(bf => bf.PartnerBusiness)
                .HasForeignKey(pbf => pbf.BusinessId);
        }
    }
}