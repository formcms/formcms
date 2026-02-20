namespace FormCMS.Utils.PageRender;

public sealed class PageTemplate
{
    private const string _mainPage =
        """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <title><!--title--></title>
            <script src="https://cdn.tailwindcss.com"></script>

            <link rel="stylesheet" href="https://unpkg.com/tailwindcss@1.4.6/dist/base.min.css">
            <link rel="stylesheet" href="https://unpkg.com/tailwindcss@1.4.6/dist/components.min.css">
            <link rel="stylesheet" href="https://unpkg.com/@tailwindcss/typography@0.1.2/dist/typography.min.css">
            <link rel="stylesheet" href="https://unpkg.com/tailwindcss@1.4.6/dist/utilities.min.css">
            <link href="https://unpkg.com/video.js@8.10.0/dist/video-js.css" rel="stylesheet" />
            
            <!--use daisy ui to display carousal-->
            <link href="https://cdn.jsdelivr.net/npm/daisyui@latest/dist/full.min.css" rel="stylesheet" type="text/css"/>
            <link rel="stylesheet" type="text/css" href="/_content/FormCMS/static-assets/css/dark.css"/>
            <style>
                /*don't delete below line, it's place holder*/
                <!--css-->
            </style>
        </head>
        <body>
        <!--Place Holder-->
        <!--body-->

        <script src="/_content/FormCMS/static-assets/js/app.js" type="module"></script>
        </body>
        </html>
        """;

    private const string _subsPage =
        """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Subscription Required</title>
            <script src="https://cdn.tailwindcss.com"></script>
        </head>
        <body class="flex items-center justify-center min-h-screen bg-gray-100">
        <div class="text-center p-6 bg-white rounded-lg shadow-lg">
            <p class="text-lg font-semibold text-gray-800 mb-4">You need to subscribe to view this page</p>
            <a href="{{url}}" class="inline-block px-6 py-2 bg-blue-600 text-white font-medium rounded hover:bg-blue-700 transition">Click here to go to subscription page</a>
        </div>
        </body>
        </html>
        """;

    public string BuildMainPage(string title, string body, string css)
        => _mainPage.Replace("<!--title-->", title).Replace("<!--body-->", body).Replace("<!--css-->", css);

    public string BuildSubsPage(string url)
        => _subsPage.Replace("{{url}}", url);
}