using Microsoft.AspNetCore.Identity;
using MYAcademy_IdentitiyEmailProject.Context;
using MYAcademy_IdentitiyEmailProject.Entities;
using MYAcademy_IdentitiyEmailProject.Models;
using MYAcademy_IdentitiyEmailProject.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<EmailContext>()
    .AddDefaultTokenProviders()
.AddErrorDescriber<TurkishIdentityErrorDescriber>();

builder.Services.AddDbContext<EmailContext>();
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(1);
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login/SignIn";
    options.AccessDeniedPath = "/ErrorPages/Page403";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/ErrorPages/Page{0}");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
