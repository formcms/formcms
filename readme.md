# FormCMS: The AI-Powered CMS

FormCMS is a cutting-edge, open-source Content Management System designed to revolutionize web development through AI. By automating the most tedious parts of development—schema design, data seeding, API creation, and UI building—FormCMS allows you to build complex, production-ready applications in minutes rather than weeks.

---

## ✨ Why FormCMS?

<table>
<tr>
<td align="center" width="33%">
<h3>🤖 AI-Powered</h3>
<p>Generate schemas, data, GraphQL queries, and full UI pages using natural language prompts. Let AI handle the tedious work while you focus on creativity.</p>
</td>
<td align="center" width="33%">
<h3>🎨 Frontend-Friendly</h3>
<p>No .NET or backend experience needed. Run FormCMS with Docker, build your app with React and AI — that's it.</p>
</td>
<td align="center" width="33%">
<h3>🚀 Scalable & Performant</h3>
<p>P95 latency under 200ms, 2,400+ QPS throughput. Handle millions of posts with CDN caching and billions of user activities with horizontal sharding.</p>
</td>
</tr>
</table>

---

## ⚡ What You Can Do with AI

FormCMS acts as your AI-driven development partner. Describe what you want, and it builds it:

- **Generate the Full Stack**: Entities (schemas), Seed Data, GraphQL Queries, and UI Pages from natural language.
- **Add Engagement Instantly**: "Add a like button" or "Show user avatar" simply works.
- **Manage & Iterate**: View version history and manage artifacts in the visual portal.

📖 **[See Wiki for full capabilities →](https://github.com/formcms/formcms/wiki/Building-a-System.md)**

---

## 🎥 In Action

Watch FormCMS build a complete Library system (Entities, Data, Queries, and UI) from scratch in under 60 seconds (sped up 10x).

![FormCMS Demo](https://github.com/formcms/formmate/blob/main/artifacts/demo_video.webp?raw=true)

---

## 🟢 Live Demo

Try the live demo at [formcms.com/mate](https://formcms.com/mate).

**Credentials:**
- **Username:** `sadmin@cms.com`
- **Password:** `Admin1!`

---

## 🚀 Quick Start (Docker)

The easiest way to get FormCMS running. **No backend setup, no .NET, no database installation required.**

Pull and run from [Docker Hub](https://hub.docker.com/repository/docker/jaike/formcms-mono):

```bash
docker run -d \
  --name formcms \
  -p 5000:5000 \
  -v formcms_data:/data \
  -e DATABASE_PROVIDER=0 \
  -e "CONNECTION_STRING=Data Source=/data/cms.db" \
  jaike/formcms-mono:latest
```

Then open **http://localhost:5000/mate** in your browser and follow the setup wizard.

> **📌 That's it!** You have a fully functional AI-powered CMS. Start building your app with React, Vite, or any frontend framework.

For production deployment with PostgreSQL, see the [Docker Hub page](https://hub.docker.com/repository/docker/jaike/formcms-mono) for a full `docker-compose.yml` example.


### 💡 Try it out
Once running, try these prompts:
- "Design entities for a library management system"
- "Add sample data for the book entity"
- "Create a query to display all available books"

🛠️ **Want to contribute or run from source?** See the [Development Setup Guide →](https://github.com/formcms/formcms/wiki/Setup.md)

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

---

## 📚 Documentation

For detailed documentation, please refer to our **[Wiki](https://github.com/formcms/formcms/wiki)** (source of truth):

| Documentation | Description |
|---------------|-------------|
| [Setup Guide](https://github.com/formcms/formcms/wiki/Setup.md) | Development environment setup |
| [Architecture](https://github.com/formcms/formcms/wiki/Architecture.md) | Component architecture and system design |
| [Orchestrator Strategy](https://github.com/formcms/formcms/wiki/Orchestrator-Strategy.md) | Multi-agent pipeline design and debugging approach |
| [Performance & Scalability](https://github.com/formcms/formcms/wiki/Performance-Scalability.md) | Benchmarks and scaling strategies |

---

## 🏗️ Architecture Overview



| Component | Description |
|-----------|-------------|
| **formmate** | AI-powered schema & UI builder |
| **formcms** | High-performance CMS backend (ASP.NET Core) |
| **AdminApp** | React admin panel for content management |
| **Portal** | User portal for history, likes, and bookmarks |

📖 **[See Wiki for detailed architecture →](https://github.com/formcms/formcms/wiki/Architecture.md)**

---

## ⚡ Performance

| Metric | Performance |
|--------|-------------|
| **P95 Latency** | < 200ms |
| **Throughput** | 2,400+ QPS per node |
| **Complex Queries** | 5-table joins over 1M rows |
| **Database Support** | SQLite, PostgreSQL, SQL Server, MySQL |

📖 **[See Wiki for performance details →](https://github.com/formcms/formcms/wiki/Performance-Scalability.md)**

---

## 🗺️ Roadmap

FormCMS is actively evolving toward a vision of **no-code app building with AI**. Here's what's coming:

| Phase | Focus | Key Features |
|-------|-------|-------------|
| **Enhanced AI** | Smarter generation | Natural language → schema, AI-suggested relationships, auto-generated CRUD & queries |
| **Visual Builder** | No-code editing | Drag-and-drop page builder, visual schema editor, real-time preview, theme templates |
| **Marketplace** | Community ecosystem | Pre-built app templates, community components, one-click install |

> **The Vision:** Describe your app in plain English → AI generates the entire backend (entities, queries, pages) → deploy with one click. No code required.