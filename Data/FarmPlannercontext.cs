using FarmPlannerAdm.Models;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FarmPlannerAdm.Data
{
    public class FarmPlannercontext : IdentityDbContext
    {
        public FarmPlannercontext(DbContextOptions<FarmPlannercontext> options)
            : base(options)
        {
        }
    }
}