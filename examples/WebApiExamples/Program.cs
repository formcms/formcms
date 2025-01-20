using System.Text.RegularExpressions;
using FormCMS;
using FormCMS.Auth.DTO;
using FormCMS.Utils.ResultExt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApiExamples;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = "Data Source=cms.db";
builder.Services.AddSqliteCms(connectionString);

//add fluent cms' permission control service 
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddCmsAuth<IdentityUser, IdentityRole, AppDbContext>();

var app = builder.Build();

//use fluent cms' CRUD 
await app.UseCmsAsync();

await app.EnsureCmsUser("sadmin@cms.com", "Admin1!", [RoleConstants.Sa]).Ok();
await app.EnsureCmsUser("admin@cms.com", "Admin1!", [RoleConstants.Admin]).Ok();

var registry = app.GetHookRegistry();
registry.EntityPreAdd.Register("teacher", addArgs =>
{
    VerifyTeacher(addArgs.RefRecord);
    return addArgs;
});
registry.EntityPreUpdate.Register("teacher", addArgs =>
{
    VerifyTeacher(addArgs.RefRecord);
    return addArgs;
});


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.Run();
return;

/////

void  VerifyTeacher(IDictionary<string,object> teacher) 
{
    var (email, phoneNumber) = ((string)teacher["email"], (string)teacher["phone_number"]);
    if (!IsValidEmail())
    {
        throw new ResultException($"email `{email}` is invalid");
    }
    if (!IsValidPhoneNumber())
    {
        throw new ResultException($"phone number `{phoneNumber}` is invalid");
    }

    return;

    bool IsValidEmail()
    {
        // Define a regex pattern for validating email addresses
        string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

        // Return true if the email matches the pattern, otherwise false
        return regex.IsMatch(email);
    }
    bool IsValidPhoneNumber()
    {
        // Define a regex pattern for validating phone numbers
        string pattern = @"^\d{10}$|^\d{3}-\d{3}-\d{4}$";
        Regex regex = new Regex(pattern);

        // Return true if the phone number matches the pattern, otherwise false
        return regex.IsMatch(phoneNumber);
    }
}