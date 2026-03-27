# FormCMS: The AI-Powered CMS

FormCMS is an open-source CMS that uses AI to generate your entire app — schemas, data, APIs, and UI — from natural language. Build production-ready applications in minutes, not weeks.

---

## ✨ Why FormCMS?

<table>
<tr>
<td align="center" width="33%">
<h3>🤖 AI-Powered</h3>
<p>Generate schemas, data, GraphQL queries, and full UI pages using natural language prompts. Let AI handle the tedious work while you focus on creativity.</p>
</td>
<td align="center" width="33%">
<h3>🎨 Zero Backend Required</h3>
<p>No .NET or backend experience needed. Run FormCMS with Docker, build your app with React and AI — that's it.</p>
</td>
<td align="center" width="33%">
<h3>🚀 Scalable & Performant</h3>
<p>P95 latency under 200ms, 2,400+ QPS throughput. Handle millions of posts with CDN caching and billions of user activities with horizontal sharding.</p>
</td>
</tr>
</table>

---

## 🎥 In Action

Watch FormCMS build a complete Library system (Entities, Data, Queries, and UI) from scratch in under 60 seconds (sped up 10x).

![FormCMS Demo](https://github.com/formcms/formmate/blob/main/artifacts/demo_video.webp?raw=true)

---

## 🎨 For Frontend Developers

**You don't need .NET or any backend experience to use FormCMS.** Just run the Docker image and start building your frontend with the tools you already know:

- Use **React + Vite** to build your app
- Use **AI (e.g., Antigravity, Cursor)** to generate schemas, queries, and pages
- FormCMS provides the backend, APIs, and admin panel — all running inside Docker

📖 **[See the Vite + React + Antigravity example →](https://github.com/formcms/formcms/wiki/Vite-React-Antigravity-Example)**

### 🏗️ Built with FormCMS

| App | Description |
|-----|-------------|
| **[Zen Health Tracker](https://zen.formcms.com/)** | A full health tracking app built in hours using FormCMS + AI coding agent — zero manual coding. |
| **Stash** | A PWA companion app for FormCMS content — bookmark, listen, and cache content with a native mobile feel. |

---

## 📱 Stash — PWA Companion App

**Stash** is a Progressive Web App (PWA) built on top of FormCMS that gives users a native mobile-app experience for consuming and managing content — no App Store required. Install it from the browser on iPhone or Android and it works just like a native app.

### Features

| Feature | What it does |
|---------|-------------|
| **Explore & Bookmarks** | Browse content and bookmark items locally — bookmarked content is cached on-device so it loads instantly with no network needed. |
| **Text-to-Speech with progress memory** | Any article or bookmark can be read aloud. The app remembers exactly where you stopped, so you can pick up later — even across sessions. |
| **Assets — Download & Convert** | Paste a URL to legally download audio/video content. Convert to **MP3** (original quality, larger file) or **M4A** (64 kbps, compressed to save space) directly from the app. |
| **Offline audio player** | Your downloaded files play fully offline. Like TTS, playback progress is saved so long audio files (lectures, podcasts) resume right where you left off. |

> **💡 Why a PWA?** Stash uses browser APIs (Web Speech, IndexedDB, Service Worker) to deliver features that feel like a native iPhone/Android app — without the overhead of publishing to an app store.
> try the pwa at https://demo.formcms.com/stash/ (no login required)
---

## 🧑‍💼 For Non-Developers

**No coding skills? No problem.** FormCMS lets you build dynamic, data-driven pages without writing a single line of code.

- Use the **built-in FormCMS Page** feature to create pages powered by your database
- Let **AI generate entire pages** for you — not just static HTML, but pages that read and display live data
- Manage your content, schemas, and pages all from the visual admin panel

> **💡 Just describe what you want** — AI handles the schema, the data, and the page. You get a fully functional, dynamic web page.

---

## 🚀 Quick Start (Docker)

The easiest way to get FormCMS running. **No backend setup, no .NET, no database installation required.**

> **🟢 Don't want to install?** Try the [live demo](https://demo.formcms.com/mate) instantly — login: `sadmin@cms.com` / `Admin1!`
> try the pwa at https://demo.formcms.com/stash/ (no login required)

Pull and run from [Docker Hub](https://hub.docker.com/repository/docker/jaike/formcms-mono):

```bash
docker run -d \
  --name formcms \
  -p 5000:5000 \
  -v formcms_data:/data \
  -e DATABASE_PROVIDER=0 \
  -e "CONNECTION_STRING=Data Source=/data/cms.db" \
  -e FORMCMS_DATA_PATH=/data \
  jaike/formcms-mono:latest
```

Then open **http://localhost:5000/mate** and follow the setup wizard.

> **📌 That's it!** Try these prompts: *"Design entities for a library system"* · *"Add sample data for books"* · *"Create a query to display all books"*

For production deployment with PostgreSQL, see the [Docker Hub page](https://hub.docker.com/repository/docker/jaike/formcms-mono). Want to contribute or run from source? See the [Development Setup Guide →](https://github.com/formcms/formcms/wiki/Setup.md)

---

## 📚 Learn More

⚡ **Performance:** P95 < 200ms · 2,400+ QPS · SQLite, PostgreSQL, SQL Server, MySQL

📖 [Documentation Wiki](https://github.com/formcms/formcms/wiki) · [Architecture](https://github.com/formcms/formcms/wiki/Architecture.md) · [Performance & Scalability](https://github.com/formcms/formcms/wiki/Performance-Scalability.md) · [Setup Guide](https://github.com/formcms/formcms/wiki/Setup.md)

---

## 🗺️ Roadmap

FormCMS is actively evolving toward a vision of **no-code app building with AI**. Here's what's coming:

| Phase | Focus | Key Features |
|-------|-------|-------------|
| **Enhanced AI** | Smarter generation | Natural language → schema, AI-suggested relationships, auto-generated CRUD & queries |
| **Visual Builder** | No-code editing | Drag-and-drop page builder, visual schema editor, real-time preview, theme templates |
| **Marketplace** | Community ecosystem | Pre-built app templates, community components, one-click install |

> **The Vision:** Describe your app in plain English → AI generates the entire backend (entities, queries, pages) → deploy with one click. No code required.