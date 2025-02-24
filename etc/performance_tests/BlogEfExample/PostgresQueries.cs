using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PostEfExample;

public static class PostgresQueries
{
   public static async Task<object> GetPostsByJoin(BloggingContext context, [FromQuery] string? last)
   {
      DateTime? ts = null;
      if (!string.IsNullOrWhiteSpace(last))
      {
         ts = JsonSerializer.Deserialize<Cursor>(CursorDecoder.Decode(last))?.Ts;
      }

      var posts = await context.Posts
         .Where(p => !p.Deleted && (ts == null || p.PublishedAt.ToLocalTime() < ts))
         .OrderByDescending(p => p.PublishedAt)
         .Take(10)
         .Select(p => new
         {
            p.Id, p.Title, p.PublishedAt, p.Slug, p.CategoryId, p.ThumbnailImage,
            CategoryIdData = context.Categories
               .Where(c => c.Id == p.CategoryId && !c.Deleted)
               .Select(c => new
               {
                  c.Id, c.Name, c.ParentCategoryId, c.FeaturedImage, c.ThumbnailImage, c.CreatedAt, c.UpdatedAt, c.Slug
               })
               .FirstOrDefault(),
            Authors = context.PostAuthors
               .Where(ap => ap.PostId == p.Id && !ap.Deleted)
               .Join(context.Authors,
                  ap => ap.AuthorId,
                  a => a.Id,
                  (ap, a) => new { a.Id, a.Name, a.Slug, a.ThumbnailImage, a.FeaturedImage, a.CreatedAt, a.UpdatedAt })
               .ToList()
         })
         .ToListAsync();
      var lastItem = new Cursor { Ts = posts.Last().PublishedAt };
      var cursor = CursorDecoder.Encode(JsonSerializer.Serialize(lastItem));
      return new { items = posts, last = cursor };
   }

   public static async Task<object> GetPostByOffset(BloggingContext context, [FromQuery] int page)
   {
      var offset = page * 10;
      var posts = await context.Posts
         .Where(p => !p.Deleted)
         .OrderByDescending(p => p.PublishedAt)
         .Skip(offset)
         .Take(10)
         .Select(p => new { p.Id, p.Title, p.PublishedAt, p.Slug, p.CategoryId, p.ThumbnailImage })
         .ToListAsync();
      var categoryIds = posts.Select(p => p.CategoryId).Distinct().ToList();

      var categories = await context.Categories
         .Where(c => categoryIds.Contains(c.Id))
         .Select(c => new
            { c.Id, c.Name, c.ParentCategoryId, c.FeaturedImage, c.ThumbnailImage, c.CreatedAt, c.UpdatedAt, c.Slug })
         .ToListAsync();

      var postIds = posts.Select(p => p.Id).ToList();

      var authors = await (from pa in context.PostAuthors
         join a in context.Authors on pa.AuthorId equals a.Id
         where postIds.Contains(pa.PostId) && !pa.Deleted && !a.Deleted
         select new
         {
            pa.PostId,
            Author = new { a.Id, a.Name, a.Slug, a.ThumbnailImage, a.FeaturedImage, a.CreatedAt, a.UpdatedAt }
         }).ToListAsync();

      var result = posts.Select(p => new
      {
         p.Id, p.Title, p.PublishedAt, p.Slug, p.CategoryId, p.ThumbnailImage,
         CategoryIdData = categories.FirstOrDefault(c => c.Id == p.CategoryId),
         Authors = authors.Where(a => a.PostId == p.Id).Select(a => a.Author).ToList()
      }).ToList();

      return result;
   }
   public static async Task<object> GetPost (BloggingContext context,[FromQuery] string? last) 
   {
      DateTime? ts = null;
      if (!string.IsNullOrWhiteSpace(last))
      {
         ts = JsonSerializer.Deserialize<Cursor>(CursorDecoder.Decode(last))?.Ts;
      }

      var posts = await context.Posts
         .Where(p => !p.Deleted && (ts == null || p.PublishedAt.ToLocalTime() < ts))
         .OrderByDescending(p => p.PublishedAt)
         .Take(10)
         .Select(p => new { p.Id, p.Title, p.PublishedAt, p.Slug, p.CategoryId, p.ThumbnailImage })
         .ToListAsync();
      var categoryIds = posts.Select(p => p.CategoryId).Distinct().ToList();

      var categories = await context.Categories
         .Where(c => categoryIds.Contains(c.Id))
         .Select(c => new { c.Id, c.Name, c.ParentCategoryId, c.FeaturedImage, c.ThumbnailImage, c.CreatedAt, c.UpdatedAt, c.Slug })
         .ToListAsync();

      var postIds = posts.Select(p => p.Id).ToList();

      var authors = await (from pa in context.PostAuthors
         join a in context.Authors on pa.AuthorId equals a.Id
         where postIds.Contains(pa.PostId) && !pa.Deleted && !a.Deleted
         select new { pa.PostId, Author = new { a.Id, a.Name, a.Slug, a.ThumbnailImage, a.FeaturedImage, a.CreatedAt, a.UpdatedAt }
         }).ToListAsync();


      var result =  posts.Select(p => new
      {
         p.Id, p.Title, p.PublishedAt, p.Slug, p.CategoryId, p.ThumbnailImage, 
         CategoryIdData = categories.FirstOrDefault(c => c.Id == p.CategoryId),
         Authors = authors.Where(a => a.PostId == p.Id).Select(a => a.Author).ToList()
      }).ToList();

      var lastItem = new Cursor{Ts = result.Last().PublishedAt}; 
      return new { items = result, last = CursorDecoder.Encode( JsonSerializer.Serialize(lastItem))};
   }
}