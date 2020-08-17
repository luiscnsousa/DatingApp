namespace DatingApp.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using AutoMapper;
    using DatingApp.API.Data;
    using DatingApp.API.Dtos;
    using DatingApp.API.Helpers;
    using DatingApp.API.Models;
    using Microsoft.AspNetCore.Mvc;
    
    [ApiController]
    [Route("api/users/{userId}/[controller]")]
    [ServiceFilter(typeof(LogUserActivity))]
    public class MessagesController : ControllerBase
    {
        private readonly IDatingRepository repo;
        private readonly IMapper mapper;

        public MessagesController(
            IDatingRepository repo,
            IMapper mapper)
        {
            this.repo = repo;
            this.mapper = mapper;
        }
        
        [HttpGet("{id}", Name = "GetMessage")]
        public async Task<IActionResult> GetMessage(int userId, int id)
        {
            if (userId != int.Parse(this.User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return this.Unauthorized();
            }

            var messageFromRepo = await this.repo.GetMessageAsync(id);
            if (messageFromRepo == null)
            {
                return this.NotFound();
            }

            return this.Ok(messageFromRepo);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessagesForUser(int userId, [FromQuery]MessageParams messageParams)
        {
            if (userId != int.Parse(this.User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return this.Unauthorized();
            }

            messageParams.UserId = userId;
            var messagesFromRepo = await this.repo.GetMessagesForUserAsync(messageParams);

            var messages = this.mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);
            
            this.Response.AddPagination(
                messagesFromRepo.CurrentPage,
                messagesFromRepo.PageSize,
                messagesFromRepo.TotalCount,
                messagesFromRepo.TotalPages);

            return this.Ok(messages);
        }
        
        [HttpGet("thread/{recipientId}", Name = "GetMessageThread")]
        public async Task<IActionResult> GetMessageThread(int userId, int recipientId)
        {
            if (userId != int.Parse(this.User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return this.Unauthorized();
            }

            var messagesFromRepo = await this.repo.GetMessageThreadAsync(userId, recipientId);

            var messageThread = this.mapper.Map<IEnumerable<MessageToReturnDto>>(messagesFromRepo);

            return this.Ok(messageThread);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId, MessageForCreationDto messageForCreationDto)
        {
            // getting the sender will help AutoMapper getting this information for messageToReturn variable
            var sender = await this.repo.GetUserAsync(userId);
            
            if (sender.Id != int.Parse(this.User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return this.Unauthorized();
            }

            messageForCreationDto.SenderId = userId;

            var recipient = await this.repo.GetUserAsync(messageForCreationDto.RecipientId);
            if (recipient == null)
            {
                return this.BadRequest("Could not find user");
            }

            var message = this.mapper.Map<Message>(messageForCreationDto);

            this.repo.Add(message);

            if (await this.repo.SaveAllAsync())
            {
                // mapping after the save will include the id of the recently saved message
                var messageToReturn = this.mapper.Map<MessageToReturnDto>(message);
                return this.CreatedAtRoute("GetMessage", new { userId, id = message.Id }, messageToReturn);
            }
            
            throw new Exception("Creating the message failed on save");
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> DeleteMessage(int userId, int id)
        {
            if (userId != int.Parse(this.User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return this.Unauthorized();
            }

            var messageFromRepo = await this.repo.GetMessageAsync(id);
            if (messageFromRepo.SenderId == userId)
            {
                messageFromRepo.SenderDeleted = true;
            }
            
            if (messageFromRepo.RecipientId == userId)
            {
                messageFromRepo.RecipientDeleted = true;
            }

            if (messageFromRepo.SenderDeleted && messageFromRepo.RecipientDeleted)
            {
                this.repo.Delete(messageFromRepo);
            }

            if (await this.repo.SaveAllAsync())
            {
                return this.NoContent();
            }
            
            throw new Exception("Error deleting the message");
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int userId, int id)
        {
            if (userId != int.Parse(this.User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return this.Unauthorized();
            }

            var message = await this.repo.GetMessageAsync(id);
            if (message.RecipientId != userId)
            {
                return this.Unauthorized();
            }

            message.IsRead = true;
            message.DateRead = DateTime.Now;

            await this.repo.SaveAllAsync();
            return this.NoContent();
        }
    }
}