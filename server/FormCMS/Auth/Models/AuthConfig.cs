namespace FormCMS.Auth.Models;

public record AuthConfig(OAuthCredential? GithubOAuthCredential = null, KeyAuthConfig? KeyAuthConfig = null);