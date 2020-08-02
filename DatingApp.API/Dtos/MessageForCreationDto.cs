namespace DatingApp.API.Dtos
{
    using System;

    public class MessageForCreationDto
    {
        public int SenderId { get; set; }

        public int RecipientId { get; set; }

        public DateTime MessageSent { get; }

        public string Content { get; set; }

        public MessageForCreationDto()
        {
            this.MessageSent = DateTime.Now;
        }
    }
}