using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostEfExample;
using Microsoft.EntityFrameworkCore;

public class BloggingContext : DbContext
{
    public DbSet<Author> Authors { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<PostAuthor> PostAuthors { get; set; }
    public BloggingContext(DbContextOptions<BloggingContext> options):base(options){}
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Database=fluent-cms;Username=postgres;Password=mysecretpassword");
    }
}
[Table("authors")]
public class Author
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("slug")]
    public string Slug { get; set; }

    [Column("bio")]
    public string Bio { get; set; }

    [MaxLength(255)]
    [Column("thumbnail_image")]
    public string ThumbnailImage { get; set; }

    [MaxLength(255)]
    [Column("featured_image")]
    public string FeaturedImage { get; set; }

    [Column("deleted")]
    public bool Deleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

[Table("categories")]
public class Category
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("slug")]
    public string Slug { get; set; }

    [Column("description")]
    public string Description { get; set; }

    [Column("parent_category_id")]
    public int? ParentCategoryId { get; set; }

    [MaxLength(255)]
    [Column("thumbnail_image")]
    public string ThumbnailImage { get; set; }

    [MaxLength(255)]
    [Column("featured_image")]
    public string FeaturedImage { get; set; }

    [Column("deleted")]
    public bool Deleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("ParentCategoryId")]
    public Category ParentCategory { get; set; }
}

[Table("posts")]
public class Post
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("title")]
    public string Title { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("slug")]
    public string Slug { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("reading_time")]
    public string ReadingTime { get; set; }

    [Required]
    [MaxLength(1000)]
    [Column("excerpt")]
    public string Excerpt { get; set; }

    [Column("content")]
    public string Content { get; set; }

    [Column("category_id")]
    public int CategoryId { get; set; }

    [MaxLength(255)]
    [Column("thumbnail_image")]
    public string ThumbnailImage { get; set; }

    [MaxLength(255)]
    [Column("featured_image")]
    public string FeaturedImage { get; set; }

    [Column("published_at")]
    public DateTime PublishedAt { get; set; }

    [Column("deleted")]
    public bool Deleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CategoryId")]
    public Category Category { get; set; }
    
    public ICollection<PostAuthor> PostAuthors { get; set; }

}

[Table("author_post")]
public class PostAuthor
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("post_id")]
    public int PostId { get; set; }

    [Column("author_id")]
    public int AuthorId { get; set; }

    [Column("deleted")]
    public bool Deleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("PostId")]
    public Post Post { get; set; }

    [ForeignKey("AuthorId")]
    public Author Author { get; set; }
}

public record Cursor
{
    public DateTime Ts { get; set; }
}