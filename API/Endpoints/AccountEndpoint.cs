using API.Common;
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
            return group;
        }
    }
}
