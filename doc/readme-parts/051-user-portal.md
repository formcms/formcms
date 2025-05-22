


---

## User Portal

<details>  
<summary>  
Users can access their view history, liked posts, bookmarked posts, and manage authentication via GitHub OAuth in a personalized portal.  
</summary> 

The **User Portal** in FormCMS provides a centralized interface for users to manage their social activity, including viewing their interaction history, liked posts, bookmarked content, and authenticating seamlessly via GitHub OAuth. This enhances user engagement by offering a tailored experience to revisit, organize content, and simplify account creation.

### History
Users can view a list of all items they have previously accessed, such as pages, posts, or other content. Each item in the history is displayed with a clickable link, allowing users to easily revisit the content.

### Liked Items
The Liked Items section displays all posts or content that the user has liked. Users can browse their liked items, with options to unlike content or click through to view the full item, fostering seamless interaction with preferred content.

### Bookmarked Items
Users can organize and view their saved content in the Bookmarked Items section. Bookmarks can be grouped into custom folders for easy categorization, enabling users to efficiently manage and access their saved items by folder or as a complete list.

### GitHub OAuth Login
The User Portal supports **GitHub OAuth** for user authentication, streamlining the login and registration process. By integrating with GitHub's OAuth system, users can log in or register using their existing GitHub credentials, eliminating the need to create and manage a separate username and password for FormCMS.

#### How It Works
- **Login/Registration**: Users are redirected to GitHub's authentication page, where they grant FormCMS permission to access their GitHub profile (e.g., username and email).
- **Account Creation**: If the user is new, FormCMS automatically creates an account using their GitHub profile information, bypassing the need for manual registration or password setup.
- **Security**: The integration uses OAuth 2.0, ensuring secure token-based authentication without storing sensitive user credentials.
- **User Experience**: Returning users can log in with a single click, leveraging their GitHub session for quick access to the portal.

#### Benefits
- **Convenience**: Users avoid the hassle of remembering a new password.
- **Speed**: Instant account creation and login enhance the onboarding experience.
- **Security**: Leverages GitHub's robust authentication system, reducing the risk of password-related vulnerabilities.

Administrators can enable or configure GitHub OAuth in the **Authentication Settings** section, where they provide the GitHub OAuth client ID and secret, and define redirect URIs for seamless integration.

### Configuration
The User Portal displays items with the following metadata:
- **Image**: A thumbnail or visual representation of the item.
- **Title**: The primary name or heading of the item.
- **Subtitle**: A brief description or secondary text for the item.
- **URL**: The link to access the full item.
- **PublishedAt**: The publication date and time of the item.

Metadata mappings are configured on the **Entity Settings** page, where administrators can define how data fields map to the portal's display. The following settings are available:

- **PageUrl**: Specifies the base URL for item links (e.g., "/content/").
- **BookmarkQuery**: Defines the query used to fetch bookmarked items.
- **BookmarkQueryParamName**: Sets the parameter name for the query (e.g., "id").
- **BookmarkTitleField**: Maps the field containing the item's title.
- **BookmarkSubtitleField**: Maps the field for the item's subtitle.
- **BookmarkImageField**: Maps the field for the item's image URL.
- **BookmarkPublishTimeField**: Maps the field for the item's publication timestamp.

These settings allow for flexible customization, ensuring the User Portal displays content accurately and consistently across history, liked items, and bookmarked items.

</details>