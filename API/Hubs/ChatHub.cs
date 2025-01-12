﻿using API.Data;
using API.DTOs;
using API.Extensions;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace API.Hubs
{
    [Authorize]
    public class ChatHub(UserManager<AppUser> userManager,AppDbContext context):Hub
    {
        public static readonly ConcurrentDictionary<string, OnlineUserDto>
            onlineUsers = new();

        public override async Task OnConnectedAsync()
        {
            var httpContext= Context.GetHttpContext();
            var receiverId = httpContext?.Request.Query["senderId"].ToString();
            var userName=Context.User!.Identity!.Name!;
            var currentUser=await userManager.FindByNameAsync(userName);
            var connectionId=Context.ConnectionId;

            if (onlineUsers.ContainsKey(userName))
            {
                onlineUsers[userName].ConnectionId = connectionId;
            }
            else
            {
                var user = new OnlineUserDto
                {
                    ConnectionId = connectionId,
                    UserName = userName,
                    ProfileImage = currentUser!.ProfileImage,
                    FullName = currentUser!.FullName,
                };

                onlineUsers.TryAdd(userName,user);
                await Clients.AllExcept(connectionId).SendAsync("Notify", currentUser);

            }
            if (!string.IsNullOrEmpty(receiverId)) 
            {
                await LoadMessage(receiverId);
            }
            await Clients.All.SendAsync("OnlineUsers", await GetAllUsers());
        }

        public async Task LoadMessage(string recipientId, int pageNumber = 1)
        {
            int pageSize = 10;
            var userName = Context.User!.Identity!.Name!;
            var currentUser=await userManager.FindByNameAsync(userName!);
            if(currentUser is null)
            {
                return;
            }
            List<MessageResponseDto> messages = await context.Messages
                .Where(
                x => x.ReceiverId == currentUser!.Id
                && x.SenderId == recipientId || x.SenderId == currentUser!.Id
                && x.ReceiverId == recipientId
                ).OrderByDescending(x => x.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(x => x.CreatedDate)
                .Select(x => new MessageResponseDto
                {
                    Id = x.Id,
                    Content = x.Content,
                    CreatedDate = x.CreatedDate,
                    ReceiverId = x.ReceiverId,
                    SenderId = x.SenderId,
                }).ToListAsync();

            foreach (var message in messages)
            {
                var msg= await context.Messages.FirstOrDefaultAsync(x=>x.Id == message.Id);
                if (msg != null && msg.ReceiverId == currentUser!.Id) 
                {
                    msg.IsRead = true;
                    await context.SaveChangesAsync();
                }
            }
            await Clients.User(currentUser.Id).SendAsync("ReceivedMessageList", messages);
        }

        public async Task SendMessage(MessageRequestDto messsage)
        {
            var senderId=Context.User!.Identity!.Name!;
            var recipientId = messsage.ReceiverId;

            var newMsg = new Message
            {
                Sender = await userManager.FindByNameAsync(senderId),
                Receiver = await userManager.FindByIdAsync(recipientId!),
                IsRead = false,
                CreatedDate = DateTime.UtcNow,
                Content = messsage.Content,
            };
            context.Messages.Add(newMsg);
            await context.SaveChangesAsync();
            await Clients.User(recipientId!).SendAsync("ReceivedNewMessage",newMsg);
        }

        public async Task NotifyTyping(string recipientUserName)
        {
            var senderUserName = Context.User!.Identity!.Name!;
            if (senderUserName is null)
            {
                return;
            }
            var connectionId = onlineUsers.Values.FirstOrDefault(x => x.UserName == recipientUserName)?.ConnectionId;
            if (connectionId != null)
            {
                await Clients.Client(connectionId).SendAsync("NotifyTypingToUser",senderUserName);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userName = Context.User!.Identity!.Name!;
            onlineUsers.TryRemove(userName, out _);
            await Clients.All.SendAsync("OnlineUsers", await GetAllUsers());
        }
        private async Task<IEnumerable<OnlineUserDto>> GetAllUsers()
        {
            var userName = Context.User!.GetUserName();
            var onlineUserSet= new HashSet<string>(onlineUsers.Keys);
            var users = await userManager.Users.Select(u => new OnlineUserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                FullName = u.FullName,
                ProfileImage = u.ProfileImage,
                IsOnline = onlineUsers.ContainsKey(u.UserName!),
                UnreadCount = context.Messages.Count(x => x.ReceiverId == userName && x.SenderId == u.Id && !x.IsRead)
            }).OrderByDescending(u => u.IsOnline).ToListAsync();
            
            return users;
        }
    }
}
