using API.Common;
using API.DTOs;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Endpoints
{
    public static class AccountEndpoint
    {
        public static RouteGroupBuilder MapAccountEndpoint(this WebApplication app)
        {
            var group = app.MapGroup("/api/account").WithTags("account");
            //Endpoint to register new user
            group.MapPost("/register", 
                async (HttpContext context,UserManager<AppUser>UserManager,
                [FromForm] string fullName,
                [FromForm] string email,
                [FromForm]string userName,
                [FromForm] IFormFile? profileImage,
                [FromForm] string password) =>
            {
                var userFromDb = await UserManager.FindByEmailAsync(email);
                if (userFromDb != null)
                {
                    return Results.BadRequest(Response<string>.Failure("User already exist."));
                }

                if(profileImage is null)
                {
                    return Results.BadRequest(Response<string>.Failure("Profile Image is Required."));
                }
                var picture = await FileUpload.Upload(profileImage);
                picture = $"{context.Request.Scheme}://{context.Request.Host}/uploads/{picture}";

                var user = new AppUser
                {
                    UserName=userName,
                    Email = email,
                    FullName = fullName,
                    ProfileImage = picture,
                };

                var result= await UserManager.CreateAsync(user,password);
                if (!result.Succeeded)
                {
                    return Results.BadRequest(Response<string>
                        .Failure(result.Errors.Select(x => x.Description)
                        .FirstOrDefault()!));
                }
                return Results.Ok(Response<string>.Success("","User Created Successfully."));
            }).DisableAntiforgery();

            //Endpoint to login as user
            group.MapPost("/login", async (UserManager<AppUser> userManager, TokenService tokenService, LoginDto dto) =>
            {
                if (dto is null)
                {
                    return Results.BadRequest(Response<string>.Failure("Invalid Login details."));
                }
                var user = await userManager.FindByEmailAsync(dto.Email);
                if (user is null)
                {
                    return Results.BadRequest(Response<string>.Failure("User not found"));
                }
                var result = await userManager.CheckPasswordAsync(user!, dto.Password);
                if (!result)
                {
                    return Results.BadRequest(Response<string>.Failure("Invalid Password"));
                }
                var token = tokenService.GenerateToken(user.Id, user.UserName!);
                return Results.Ok(Response<string>.Success(token, "Login Successfully"));
            });
            return group;
        }
    }
}
