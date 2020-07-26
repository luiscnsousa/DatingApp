namespace DatingApp.API.Helpers
{
    public class UserParams
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

        public string Gender { get; set; }

        public int MinAge { get; set; } = 18;

        public int MaxAge { get; set; } = 99;

        public string OrderBy { get; set; }

        public bool Likees { get; set; } = false;

        public bool Likers { get; set; } = false;
    }
}