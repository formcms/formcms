namespace FormCMS.Core.Descriptors;

public sealed record Page(
    string Name,
    string Title,
    string? Query,
    string Html,
    string Css,
    /*for grapes.js restore last configure */
    string Components,
    string Styles);
