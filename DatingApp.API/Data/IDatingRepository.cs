namespace DatingApp.API.Data
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DatingApp.API.Helpers;
    using DatingApp.API.Models;

    public interface IDatingRepository
    {
        void Add<T>(T entity) where T: class;
        
        void Delete<T>(T entity) where T: class;

        Task<bool> SaveAllAsync();
        
        Task<PagedList<User>> GetUsersAsync(UserParams userParams);
        
        Task<User> GetUserAsync(int id, bool isCurrentUser);
        
        Task<Photo> GetPhotoAsync(int id);
        
        Task<Photo> GetMainPhotoForUserAsync(int userId);

        Task<Like> GetLikeAsync(int userId, int recipientId);

        Task<Message> GetMessageAsync(int id);

        Task<PagedList<Message>> GetMessagesForUserAsync(MessageParams messageParams);

        Task<List<Message>> GetMessageThreadAsync(int userId, int recipientId);
    }
}