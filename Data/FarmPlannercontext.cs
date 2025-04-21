using ADUSAdm.Models;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ADUSAdm.Data
{
    public class ADUScontext : IdentityDbContext
    {
        public ADUScontext(DbContextOptions<ADUScontext> options)
            : base(options)
        {
        }
    }
}