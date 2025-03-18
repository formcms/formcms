using FormCMS.Auth.ApiClient;
using FormCMS.Core.Descriptors;
using FormCMS.Core.Assets;
using FormCMS.CoreKit.ApiClient;
using FormCMS.Utils.DisplayModels;
using FormCMS.Utils.jsonElementExt;
using FormCMS.Utils.RecordExt;
using FormCMS.Utils.ResultExt;
using Humanizer;
using NUlid;
using SkiaSharp;
using Attribute = FormCMS.Core.Descriptors.Attribute;

namespace FormCMS.Course.Tests;

public class AssetApiTest
{
    private readonly AssetApiClient _assetApiClient;
    private readonly SchemaApiClient _schemaApiClient;
    private readonly EntityApiClient _entityApiClient;
    private readonly  string _post = "at_post_" + Ulid.NewUlid();
 
    public AssetApiTest()
    {
        Util.SetTestConnectionString();
        var webAppClient = new WebAppClient<Program>();
        new AuthApiClient(webAppClient.GetHttpClient()).EnsureSaLogin().Ok().GetAwaiter().GetResult();

        _schemaApiClient = new SchemaApiClient(webAppClient.GetHttpClient());
        _entityApiClient = new EntityApiClient(webAppClient.GetHttpClient());
        _assetApiClient = new AssetApiClient(webAppClient.GetHttpClient());
    }

    [Fact]
    public async Task GetEntity()
    {
        var entity = await _assetApiClient.GetEntity(true).Ok();
        Assert.NotNull(entity.Attributes.FirstOrDefault(x=>x.Field == nameof(Asset.LinkCount).Camelize()));
        
        entity = await _assetApiClient.GetEntity(false).Ok();
        Assert.Null(entity.Attributes.FirstOrDefault(x=>x.Field == nameof(Asset.LinkCount).Camelize()));
    }

    [Fact]
    public async Task ListWithLinkCount()
    {
        string txtFileName = $"{Ulid.NewUlid()}.txt";
        var bs = System.Text.Encoding.UTF8.GetBytes("test".ToCharArray());
        await _assetApiClient.AddAsset([(txtFileName, bs)]).Ok();
        var res = await _assetApiClient.List(true, "").Ok();
        Assert.True(res.Items[0].ContainsKey(nameof(Asset.LinkCount).Camelize()));
    }

    [Fact]
    public async Task TestBaseUrl()
    {
        await _assetApiClient.GetEntityBaseUrl().Ok();
    }

    [Fact]
    public async Task SingleByPath()
    {
         var txtFileName = $"{Ulid.NewUlid()}.txt";
         var bs = System.Text.Encoding.UTF8.GetBytes("test".ToCharArray());
         var path = await _assetApiClient.AddAsset([(txtFileName, bs)]).Ok();
         var rec = await _assetApiClient.Single(path).Ok();
         Assert.NotNull(rec);

    }
    [Fact]
    public async Task Replace()
    {
        var txtFileName = $"{Ulid.NewUlid()}.txt";
        var bs = System.Text.Encoding.UTF8.GetBytes("test".ToCharArray());
        await _assetApiClient.AddAsset([(txtFileName, bs)]).Ok();
        var id = await _assetApiClient.GetAssetIdByName(txtFileName);
      
        txtFileName = $"{Ulid.NewUlid()}.txt";
        bs = System.Text.Encoding.UTF8.GetBytes("test".ToCharArray());
        await _assetApiClient.Replace(id, txtFileName, bs).Ok();
        var asset = await _assetApiClient.Single(id).Ok();
        Assert.Equal(txtFileName,asset.Name);
        Assert.Equal(4,asset.Size);
    }

    [Fact]
    public async Task TestMeta()
    {
        var txtFileName = $"{Ulid.NewUlid()}.txt";
        var bs = System.Text.Encoding.UTF8.GetBytes("test".ToCharArray());
        await _assetApiClient.AddAsset([(txtFileName, bs)]).Ok();
        var id = await _assetApiClient.GetAssetIdByName(txtFileName);
        
        var asset = await _assetApiClient.Single(id).Ok();
        asset = asset with
        {
            Title = "Title", Metadata = new Dictionary<string, object>
            {
                {"color","yellow"}
            }
        };
        await _assetApiClient.UpdateMeta(asset).Ok();
        asset = await _assetApiClient.Single(id).Ok();
        Assert.Equal("Title",asset.Title);
        Assert.True(asset.Metadata.ContainsKey("color"));
    }

    [Fact]
    public async Task CreateAsset()
    {
        var path = await _assetApiClient.AddAsset([("image.png",Create2048TileImage())]).Ok();
        var res = await _assetApiClient.List(false,$"path[equals]={path}").Ok();
        Assert.Single(res.Items); 
    }

    private async Task<AssetLink?> EntityHasAsset(string assetName, string entityName, long recordId)
    {
        var list = await _assetApiClient.List(false,$"name[equals]={assetName}").Ok();
        var id = list.Items[0].LongOrZero("id");
        var asset = await _assetApiClient.Single(id).Ok();
        return asset.Links?.FirstOrDefault(x => x.EntityName == entityName && x.RecordId == recordId);
    }

    [Fact]
    public async Task AddDataWithFileAsset()
    {
        await _schemaApiClient.EnsureEntity(_post, "title", false,
            new Attribute("title", "Title"),
            new Attribute("file", "File", DataType.String, DisplayType.File),
            new Attribute("image", "Image", DataType.String, DisplayType.Image),
            new Attribute("images", "Images", DataType.String, DisplayType.Gallery)
        ).Ok();

        string txtFileName = $"{Ulid.NewUlid()}.txt";
        var bs = System.Text.Encoding.UTF8.GetBytes("test".ToCharArray());
        var file = await _assetApiClient.AddAsset([(txtFileName, bs)]).Ok();

        var singleImage = $"{Ulid.NewUlid()}.jpg";
        var path = await _assetApiClient.AddAsset([(singleImage, Create2048TileImage())]).Ok();

        var img1 = $"{Ulid.NewUlid()}.jpg";
        var img2 = $"{Ulid.NewUlid()}.jpg";
        var imagesPath = await _assetApiClient.AddAsset([
            (img1, Create2048TileImage()),
            (img2, Create2048TileImage())
        ]).Ok();

        var res = await _entityApiClient.Insert(_post, 
            new { title = "post1", file, image = path, images = imagesPath }).Ok();
        var id = res.GetProperty("id").GetInt64();

        Assert.NotNull(await EntityHasAsset(txtFileName, _post, id));
        Assert.NotNull(await EntityHasAsset(singleImage, _post, id));
        Assert.NotNull(await EntityHasAsset(img1, _post, id));
        Assert.NotNull(await EntityHasAsset(img2, _post, id));

        var ele = await _entityApiClient.Single(_post, id).Ok();
        var record = ele.ToDictionary();

        var updateImage = $"{Ulid.NewUlid()}.jpg";
        imagesPath = await _assetApiClient.AddAsset([
            (updateImage, Create2048TileImage()),
        ]).Ok();
        record["images"] = imagesPath;
        await _entityApiClient.Update(_post, record).Ok();

        Assert.NotNull(await EntityHasAsset(txtFileName, _post, id));
        Assert.NotNull(await EntityHasAsset(singleImage, _post, id));
        Assert.NotNull(await EntityHasAsset(updateImage, _post, id));
        Assert.Null(await EntityHasAsset(img1, _post, id));
        Assert.Null(await EntityHasAsset(img2, _post, id));


        ele = await _entityApiClient.Single(_post, id).Ok();
        await _entityApiClient.Delete(_post, ele).Ok();
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