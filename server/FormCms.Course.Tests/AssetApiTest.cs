using FormCMS.Core.Descriptors;
using FormCMS.Core.Assets;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.jsonElementExt;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;
using NUlid;
using SkiaSharp;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Course.Tests;
[Collection("API")]
public class AssetApiTest(AppFactory factory)
{
    private readonly  string _post = "asset_post" + Ulid.NewUlid();
    private bool _ = factory.LoginAndInitTestData();
    
    [Fact]
    public async Task GetEntity()
    {
        var entity = await factory.AssetApi.GetEntity(true).Ok();
        Assert.NotNull(entity.Attributes.FirstOrDefault(x=>x.Field == nameof(Asset.LinkCount).Camelize()));
        
        entity = await  factory.AssetApi.GetEntity(false).Ok();
        Assert.Null(entity.Attributes.FirstOrDefault(x=>x.Field == nameof(Asset.LinkCount).Camelize()));
    }

    [Fact]
    public async Task ListWithLinkCount()
    {
        string txtFileName = $"{Ulid.NewUlid()}.txt";
        var bs = System.Text.Encoding.UTF8.GetBytes("test".ToCharArray());
        await  factory.AssetApi.AddAsset([(txtFileName, bs)]).Ok();
        var res = await  factory.AssetApi.List(true, "").Ok();
        Assert.True(res.Items[0].ContainsKey(nameof(Asset.LinkCount).Camelize()));
    }

    [Fact]
    public async Task TestBaseUrl()
    {
        await  factory.AssetApi.GetEntityBaseUrl().Ok();
    }

    [Fact]
    public async Task SingleByPath()
    {
         var txtFileName = $"{Ulid.NewUlid()}.txt";
         var bs = System.Text.Encoding.UTF8.GetBytes("test".ToCharArray());
         var path = await  factory.AssetApi.AddAsset([(txtFileName, bs)]).Ok();
         var rec = await  factory.AssetApi.Single(path).Ok();
         Assert.NotNull(rec);

    }
    [Fact]
    public async Task Replace()
    {
        var txtFileName = $"{Ulid.NewUlid()}.txt";
        var bs = System.Text.Encoding.UTF8.GetBytes("test".ToCharArray());
        await  factory.AssetApi.AddAsset([(txtFileName, bs)]).Ok();
        var id = await  factory.AssetApi.GetAssetIdByName(txtFileName);
      
        txtFileName = $"{Ulid.NewUlid()}.txt";
        bs = System.Text.Encoding.UTF8.GetBytes("test".ToCharArray());
        await  factory.AssetApi.Replace(id, txtFileName, bs).Ok();
        var asset = await  factory.AssetApi.Single(id).Ok();
        Assert.Equal(txtFileName,asset.Name);
        Assert.Equal(4,asset.Size);
    }

    [Fact]
    public async Task TestMeta()
    {
        var txtFileName = $"{Ulid.NewUlid()}.txt";
        var bs = System.Text.Encoding.UTF8.GetBytes("test".ToCharArray());
        await  factory.AssetApi.AddAsset([(txtFileName, bs)]).Ok();
        var id = await  factory.AssetApi.GetAssetIdByName(txtFileName);
        
        var asset = await  factory.AssetApi.Single(id).Ok();
        asset = asset with
        {
            Title = "Title", Metadata = new Dictionary<string, object>
            {
                {"color","yellow"}
            }
        };
        await  factory.AssetApi.UpdateMeta(asset).Ok();
        asset = await  factory.AssetApi.Single(id).Ok();
        Assert.Equal("Title",asset.Title);
        Assert.True(asset.Metadata.ContainsKey("color"));
    }

    [Fact]
    public async Task CreateAsset()
    {
        var path = await  factory.AssetApi.AddAsset([("image.png",Create2048TileImage())]).Ok();
        var res = await  factory.AssetApi.List(false,$"path[equals]={path}").Ok();
        Assert.Single(res.Items); 
    }

    private async Task<AssetLink?> EntityHasAsset(string assetName, string entityName, long recordId)
    {
        var list = await  factory.AssetApi.List(false,$"name[equals]={assetName}").Ok();
        var id = list.Items[0].LongOrZero("id");
        var asset = await  factory.AssetApi.Single(id).Ok();
        return asset.Links?.FirstOrDefault(x => x.EntityName == entityName && x.RecordId == recordId);
    }

    [Fact]
    public async Task AddDataWithFileAsset()
    {
        await  factory.SchemaApi.EnsureEntity(_post, "title", false,
            new Attribute("title", "Title"),
            new Attribute("file", "File", DataType.String, DisplayType.File),
            new Attribute("image", "Image", DataType.String, DisplayType.Image),
            new Attribute("images", "Images", DataType.String, DisplayType.Gallery)
        ).Ok();

        string txtFileName = $"{Ulid.NewUlid()}.txt";
        var bs = System.Text.Encoding.UTF8.GetBytes("test".ToCharArray());
        var file = await  factory.AssetApi.AddAsset([(txtFileName, bs)]).Ok();

        var singleImage = $"{Ulid.NewUlid()}.jpg";
        var path = await  factory.AssetApi.AddAsset([(singleImage, Create2048TileImage())]).Ok();

        var img1 = $"{Ulid.NewUlid()}.jpg";
        var img2 = $"{Ulid.NewUlid()}.jpg";
        var imagesPath = await  factory.AssetApi.AddAsset([
            (img1, Create2048TileImage()),
            (img2, Create2048TileImage())
        ]).Ok();

        var res = await  factory.EntityApi.Insert(_post, 
            new { title = "post1", file, image = path, images = imagesPath }).Ok();
        var id = res.GetProperty("id").GetInt64();

        Assert.NotNull(await EntityHasAsset(txtFileName, _post, id));
        Assert.NotNull(await EntityHasAsset(singleImage, _post, id));
        Assert.NotNull(await EntityHasAsset(img1, _post, id));
        Assert.NotNull(await EntityHasAsset(img2, _post, id));

        var ele = await  factory.EntityApi.Single(_post, id).Ok();
        var record = ele.ToDictionary();

        var updateImage = $"{Ulid.NewUlid()}.jpg";
        imagesPath = await  factory.AssetApi.AddAsset([
            (updateImage, Create2048TileImage()),
        ]).Ok();
        record["images"] = imagesPath;
        await  factory.EntityApi.Update(_post, record).Ok();

        Assert.NotNull(await EntityHasAsset(txtFileName, _post, id));
        Assert.NotNull(await EntityHasAsset(singleImage, _post, id));
        Assert.NotNull(await EntityHasAsset(updateImage, _post, id));
        Assert.Null(await EntityHasAsset(img1, _post, id));
        Assert.Null(await EntityHasAsset(img2, _post, id));


        ele = await  factory.EntityApi.Single(_post, id).Ok();
        await  factory.EntityApi.Delete(_post, ele).Ok();
        Assert.Null(await EntityHasAsset(txtFileName, _post, id));
        Assert.Null(await EntityHasAsset(singleImage, _post, id));
        Assert.Null(await EntityHasAsset(updateImage, _post, id));
        Assert.Null(await EntityHasAsset(img1, _post, id));
        Assert.Null(await EntityHasAsset(img2, _post, id));
    }

    private byte[] Create2048TileImage()
    {
        using var bitmap = new SKBitmap(2048, 2048);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black); // SKColors.Black is (0, 0, 0, 255)
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}