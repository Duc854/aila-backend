using AILA.Application.Common.Interfaces.Repositories;
using AILA.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Repositories
{
    public class AIPracticeMaterialRepository
        : GenericRepository<AIPracticeMaterial>,
          IAIPracticeMaterialRepository
    {
        public AIPracticeMaterialRepository(ApplicationDbContext context)
            : base(context)
        {
        }
    }
}
