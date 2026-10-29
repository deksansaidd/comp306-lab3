using LearningPortal.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Amazon.S3;
using LearningPortal.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IAmazonS3>(_ => AwsFactory.CreateS3());
builder.Services.AddScoped<IFileStorage, S3FileStorage>();
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("LearningPortal");
builder.Services.AddDbContext<LearningPortalContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Resources}/{action=Index}/{id?}");

app.Run();