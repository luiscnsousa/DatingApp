namespace DatingApp.API.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DatingApp.API.Models;
    using Microsoft.EntityFrameworkCore;

    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext context;

        public DatingRepository(DataContext context)
        {
            this.context = context;
        }
        
        public void Add<T>(T entity) where T : class
        {
            this.context.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            this.context.Remove(entity);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await this.context.SaveChangesAsync() > 0;
        }

        public Task<List<User>> GetUsersAsync()
        {
            return this.context.Users
                .Include(u => u.Photos)
                .ToListAsync();
        }

        public Task<User> GetUserAsync(int id)
        {
            return this.context.Users
                .Include(u => u.Photos)
                .FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}