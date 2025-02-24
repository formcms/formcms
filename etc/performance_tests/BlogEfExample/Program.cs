using Microsoft.EntityFrameworkCore;
using PostEfExample;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<BloggingContext>(options =>
    options.UseNpgsql(Environment.GetEnvironmentVariable("Postgres") 
                      ?? builder.Configuration.GetConnectionString("Postgres")));

var app = builder.Build();

app.MapGet("/posts_join", PostgresQueries.GetPostsByJoin);

app.MapGet("/posts_offset", PostgresQueries.GetPostByOffset);

app.MapGet("/posts", PostgresQueries.GetPost);

app.Run();
