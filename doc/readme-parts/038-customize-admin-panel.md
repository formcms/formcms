




---

## Customizing the Admin Panel
<details>  
<summary>  
FormCms' modular component structure makes it easy to modify UI text, replace components, or swap entire pages.  
</summary>  

### FormCmsAdminSdk and FormCmsAdminApp

The FormCms Admin Panel is built with React and split into two projects:

- **[FormCmsAdminSdk](https://github.com/FormCms/FormCmsAdminSdk)**  
  This SDK handles backend interactions and complex state management. It is intended to be a submodule of your own React App. It follows a minimalist approach, relying only on:
  - `"react"`, `"react-dom"`, `"react-router-dom"`: Essential React and routing dependencies.
  - `"axios"` and `"swr"`: For API access and state management.
  - `"qs"`: Converts query objects to strings.
  - `"react-hook-form"`: Manages form inputs.

- **[FormCmsAdminApp](https://github.com/FormCms/FormCmsAdminApp)**  
  A demo implementation showing how to build a React app with the FormCmsAdminSdk. Fork this project to customize the layout, UI text, or add features.

### Why is FormCmsAdminSdk a Submodule Instead of an NPM package?

A **Git submodule** embeds an external repository (e.g., [FormCmsAdminSdk](https://github.com/FormCms/FormCmsAdminSdk)) as a subdirectory in your project. Unlike NPM packages, which deliver bundled code, submodules provide the full, readable source, pinned to a specific commit. This offers flexibility for customization, debugging, or upgrading the SDK directly in your repository.

To update a submodule:
```
git submodule update --remote
```  
Then commit the updated reference in your parent repository.

### Setting Up Your Custom AdminPanelApp

To create a custom AdminPanelApp with submodules, start with the example repo [FormCmsAdminApp](https://github.com/FormCms/FormCmsAdminApp):
```
git clone --recurse-submodules https://github.com/FormCms/FormCmsAdminApp.git
```  
The `--recurse-submodules` flag ensures the SDK submodule is cloned alongside the main repo.
```
cd FormCmsAdminApp
pnpm install
```  
Start the formCms backend, you might need to modify .env.development, change the Api url to your backend.
```
VITE_REACT_APP_API_URL='http://127.0.0.1:5000/api'
```  
Start the React App
```
pnpm dev
```

### Deploying Your Customized Admin Panel

After customizing, build your app:
```
pnpm build
```  
Copy the contents of the `dist` folder to `<your backend project>\wwwroot\admin` to replace the default Admin Panel.

### Customizing Layout and Logo

The SDK ([FormCmsAdminSdk](https://github.com/FormCms/FormCmsAdminSdk)) includes an integrated router and provides three hooks for menu items:
- `useEntityMenuItems`
- `useAssetMenuItems`
- `useSystemMenuItems`

Use these to design your app’s layout and update the logo within this structure.

### Modifying Page Text

Each page (a root-level component tied to the router) can use a corresponding `use***Page` hook from the SDK. These hooks handle state and API calls, returning components for your UI.

To customize text:
- Pass specific prompts and labels via the `pageConfig` argument in the hook.
- For text shared across pages, use the `componentConfig` argument.

### Swapping UI Components

Replace table columns, input fields, or other UI components with your custom versions as needed.
</details>  

---
