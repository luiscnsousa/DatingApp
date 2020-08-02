namespace DatingApp.API.Helpers
{
    public class MessageParams
    {
        private const int MaxPageSize = 50;
        
        private int pageSize = 10;

        public int PageSize
        {
            get => this.pageSize;
            set => this.pageSize = (value > MaxPageSize ? MaxPageSize : value);
        }
        
        public int PageNumber { get; set; } = 1;

        public int UserId { get; set; }

        public string MessageContainer { get; set; } = "Unread";
    }
}