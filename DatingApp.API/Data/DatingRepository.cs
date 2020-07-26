namespace DatingApp.API.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using DatingApp.API.Helpers;
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

        public async Task<PagedList<User>> GetUsersAsync(UserParams userParams)
        {
            var users = this.context.Users
                .Include(u => u.Photos)
                .OrderByDescending(u => u.LastActive)
                .Where(u => u.Id != userParams.UserId)
                .Where(u => u.Gender == userParams.Gender)
                .AsQueryable();

            if (userParams.Likers)
            {
                var userLikers = await this.GetUserLikesAsync(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikers.Contains(u.Id));
            }
            
            if (userParams.Likees)
            {
                var userLikees = await this.GetUserLikesAsync(userParams.UserId, userParams.Likers);
                users = users.Where(u => userLikees.Contains(u.Id));
            }

            if (userParams.MinAge != 18 || userParams.MaxAge != 99)
            {
                var minDateOfBirth = DateTime.Today.AddYears(-userParams.MaxAge - 1);
                var maxDateOfBirth = DateTime.Today.AddYears(-userParams.MinAge);

                users = users.Where(u => u.DateOfBirth >= minDateOfBirth && u.DateOfBirth <= maxDateOfBirth);
            }

            if (!string.IsNullOrEmpty(userParams.OrderBy))
            {
                switch (userParams.OrderBy)
                {
                    case "created":
                        users = users.OrderByDescending(u => u.Created);
                        break;
                    default:
                        users = users.OrderByDescending(u => u.LastActive);
                        break;
                }
            }
            
            return await PagedList<User>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);
        }

        public Task<User> GetUserAsync(int id)
        {
            return this.context.Users
                .Include(u => u.Photos)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public Task<Photo> GetPhotoAsync(int id)
        {
            return this.context.Photos.FirstOrDefaultAsync(p => p.Id == id);
        }

        public Task<Photo> GetMainPhotoForUserAsync(int userId)
        {
            return this.context.Photos.FirstOrDefaultAsync(p => p.UserId == userId && p.IsMain);
        }

        public Task<Like> GetLikeAsync(int userId, int recipientId)
        {
            return this.context.Likes.FirstOrDefaultAsync(u => u.LikerId == userId && u.LikeeId == recipientId);
        }

        private async Task<IEnumerable<int>> GetUserLikesAsync(int id, bool likers)
        {
            var user = await this.context.Users
                .Include(u => u.Likers)
                .Include(u => u.Likees)
                .FirstOrDefaultAsync(u => u.Id == id);

            return likers
                ? user.Likers.Where(u => u.LikeeId == id).Select(l => l.LikerId)
                : user.Likees.Where(u => u.LikerId == id).Select(l => l.LikeeId);
        }
    }
}