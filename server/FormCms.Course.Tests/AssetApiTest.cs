using FormCMS.Auth.ApiClient;
using FormCMS.Core.Descriptors;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.jsonElementExt;
using FormCMS.Utils.ResultExt;
using NUlid;
using SkiaSharp;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Course.Tests;

public class AssetApiTest
{
    private readonly AssetApiClient _assetApiClient;
    private readonly AccountApiClient _accountApiClient;
    private readonly SchemaApiClient _schemaApiClient;
    private readonly EntityApiClient _entityApiClient;
    private readonly  string _post = "at_post_" + Ulid.NewUlid();
 
    public AssetApiTest()
    {
        Util.SetTestConnectionString();
        var webAppClient = new WebAppClient<Program>();
        new AuthApiClient(webAppClient.GetHttpClient()).EnsureSaLogin().Ok().GetAwaiter().GetResult();

        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _accountApiClient = new AccountApiClient(webAppClient.GetHttpClient());
        _entityApiClient = new EntityApiClient(webAppClient.GetHttpClient());
        _assetApiClient = new AssetApiClient(webAppClient.GetHttpClient());
    }

   
    [Fact]
    public async Task CreateAsset()
    {
        var path = await _assetApiClient.AddAsset([("image.png",Create2048TileImage())]).Ok();
        var res = await _assetApiClient.List($"path[equals]={path}").Ok();
        Assert.Single(res.Items); 
    }

    [Fact]
    public async Task AddDataWithFileAsset()
    {
        await _schemaApiClient.EnsureEntity(_post, "title", false,
            new Attribute("title","Title"),
            new Attribute("file","File", DataType.String, DisplayType.File),
            new Attribute("image","Image", DataType.String, DisplayType.Image),
            new Attribute("images","Images", DataType.String, DisplayType.Gallery)
            ).Ok();
        
        const string fileName = "test.txt";
        var bs = System.Text.Encoding.UTF8.GetBytes("test".ToCharArray());
        var file = await _assetApiClient.AddAsset([(fileName,bs)]).Ok();

        var image = await _assetApiClient.AddAsset([("image.png", Create2048TileImage())]).Ok();
        var images = await _assetApiClient.AddAsset([
            ("image1.png", Create2048TileImage()),
            ("image2.png", Create2048TileImage())
        ]).Ok();

        var title = "post1";
        var res = await _entityApiClient.Insert(_post, new {title,file,image, images}).Ok();
        //todo: check 4 more assetLink records added.
        
        var id = res.GetProperty("id").GetInt64();
        var ele = await _entityApiClient.Single(_post, id).Ok();
        var record = ele.ToDictionary();
        
        images = await _assetApiClient.AddAsset([
            ("image3.png", Create2048TileImage()),
            ("image4.png", Create2048TileImage()),
            ("image5.png", Create2048TileImage())
        ]).Ok();        
        record["images"] = images;
        await _entityApiClient.Update(_post, record).Ok();
        //todo: check now 5 assetLink related to this record
        
         ele = await _entityApiClient.Single(_post, id).Ok();
         await _entityApiClient.Delete(_post, ele).Ok();
         //todo: now no assetLink related to this record
    }
    
    private byte[] Create2048TileImage()
    {
        // Create a 512x512 bitmap
        using var bitmap = new SKBitmap(512, 512);
        using var canvas = new SKCanvas(bitmap);

        // Set background color (typical 2048 beige)
        canvas.Clear(new SKColor(250, 248, 239));
        // Draw tile background (like a "2048" value tile)
        using var paint = new SKPaint
        {
            Color = new SKColor(238, 228, 218), // Light beige for 2048 tile
            Style = SKPaintStyle.Fill
        };
    
        // Draw rounded rectangle for tile
        var rect = SKRect.Create(156, 156, 200, 200);
        canvas.DrawRoundRect(rect, 20, 20, paint);

        // Draw number
        using var textPaint = new SKPaint
        {
            Color = new SKColor(119, 110, 101), // Dark text color
            TextSize = 100,
            TextAlign = SKTextAlign.Center,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
    
        canvas.DrawText("2048", 256, 306, textPaint);

        // Convert to byte array
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}