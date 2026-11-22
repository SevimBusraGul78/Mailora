using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MYAcademy_IdentitiyEmailProject.Entities;

namespace MYAcademy_IdentitiyEmailProject.Context
{
    public class EmailContext:IdentityDbContext<AppUser>
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=SEVIMBUSRA\\SQLEXPRESS;Initial Catalog=IdentityEmailDb;Integrated Security=True;TrustServerCertificate=True");
        }
        public DbSet<Category> Categories { get; set; } 
        public DbSet<Message> Messages { get; set; }

    }
}
