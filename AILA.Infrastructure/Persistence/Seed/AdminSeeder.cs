using AILA.Application.Common.Interfaces;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Infrastructure.Persistence.Seed
{
    public class AdminSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly AdminAccountSettings _adminSettings;

        public AdminSeeder(
            ApplicationDbContext context,
            IPasswordHasher passwordHasher,
            IOptions<AdminAccountSettings> adminOptions)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _adminSettings = adminOptions.Value;
        }

        public async Task SeedAsync()
        {
            bool adminExists = await _context.Users
                .AnyAsync(u => u.Role == UserRole.Admin);

            if (adminExists) return; // Chỉ seed nếu chưa có Admin nào

            var passwordHash = _passwordHasher.HashPassword(_adminSettings.Password);

            var admin = new User(
                email: _adminSettings.Email,
                fullName: _adminSettings.FullName,
                role: UserRole.Admin,
                passwordHash: passwordHash
            );

            _context.Users.Add(admin);
            await _context.SaveChangesAsync();
        }
    }
}
