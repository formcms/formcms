



---

## Customizing the Admin Panel
<details>
<summary>
FormCms' Modular Component Structure simplifies modifying UI text, replacing UI components, and swapping entire pages.
</summary>

### Using Submodules Instead of NPM
A **Git submodule** is a feature in Git that allows you to embed one Git repository inside another as a subdirectory. It’s a way to include an external repository’s code (like the FormCms SDK, available at [https://github.com/FormCms/FormCmsAdminSdk](https://github.com/FormCms/FormCmsAdminSdk)) within your project while keeping its history and updates separate. Unlike installing a package via `npm`, which provides a bundled, often minified version, a submodule gives you the full, readable source code. The submodule is an independent repository pinned to a specific commit, ensuring consistency until you choose to update it. This makes it ideal for customization, debugging, or upgrading the SDK version directly in your project.

To update a submodule to a newer version, you can run:
```
git submodule update --remote
```  
Then commit the updated reference in your parent repository.

### Creating Your Own AdminPanelApp
To set up your custom AdminPanelApp with submodules, use the example repository [https://github.com/FormCms/FormCmsAdminApp](https://github.com/FormCms/FormCmsAdminApp) and follow these steps:
```
git clone --recurse-submodules https://github.com/FormCms/FormCmsAdminApp.git
cd FormCmsAdminApp
pnpm install
pnpm dev
```  
The `--recurse-submodules` flag ensures the submodules (including the SDK from [https://github.com/FormCms/FormCmsAdminSdk](https://github.com/FormCms/FormCmsAdminSdk)) are cloned along with the main repository.

### Replacing the Default AdminPanel with Your Customized Version
After customizing your AdminPanelApp, build it using:
```
pnpm build
```  
Then, copy all files from the `dist` directory to `<your backend project>\wwwroot\admin`.

### Customizing the App Layout and Logo
The router is integrated into the SDK ([https://github.com/FormCms/FormCmsAdminSdk](https://github.com/FormCms/FormCmsAdminSdk)), and `FormCmsAdminSdk` provides three hooks to retrieve menu items:
- `useEntityMenuItems`
- `useAssetMenuItems`
- `useSystemMenuItems`

You can use these menu items to design your app’s entry layout UI and update the app logo within this structure.

### Modifying Page Language
For each page (a root-level component tied to the router), you can use a `use***Page` hook from the SDK ([https://github.com/FormCms/FormCmsAdminSdk](https://github.com/FormCms/FormCmsAdminSdk)). This hook manages state and API calls, returning UI components for you to build your interface.

To customize text:
- Pass prompt and label text via the `pageConfig` argument in the hook.
- For shared prompts or labels across multiple pages, use the `componentConfig` argument.

### Replacing UI Components
You can swap out table column components or input components with your own custom components as needed.
</details>
