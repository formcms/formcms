# FormCMS: The AI-Powered CMS

FormCMS is a cutting-edge, open-source Content Management System designed to revolutionize web development through AI. By automating the most tedious parts of development—schema design, data seeding, API creation, and UI building—FormCMS allows you to build complex, production-ready applications in minutes rather than weeks.

---

## ⚡ Powering Your Workflow with AI

FormCMS isn't just a place to store content; it's an AI-driven development partner. 

### 1. Generate Entity (Schema)
Forget manual table definitions. Simply describe your business domain (e.g., "I need a system to manage a digital library with books, authors, and rentals"), and FormCMS's AI will:
- Design the normalized database schema.
- Establish relationships (Many-to-One, Many-to-Many).
- Configure appropriate data types (Strings, Numbers, Lookups, Junctions).

### 2. Generate Data (Seeding)
Tired of "Lorem Ipsum"? Use AI to generate realistic, high-quality sample data:
- Populate your database with meaningful records.
- Preserve relational integrity across entities.
- Test your UI with data that looks and feels real.

### 3. Generate Query (API)
Writing GraphQL can be complex. In FormCMS, you can:
- Prompt the AI to build logic: "Give me all books published after 2020 by authors with more than 5 stars."
- The AI generates the GraphQL query and converts it into a secure, high-performance REST endpoint automatically.

### 4. Generate Page (UI)
Go from prompt to page instantly:
- "Build a landing page for my library that sections books by genre and features a search bar."
- AI generates the HTML/CSS using semantic structures and bridges it with your data queries.

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

## 🚀 Quick Start

Get the project running locally in 4 steps.

### 1. Clone Repositories
You'll need both the core CMS and the AI agent.
```bash
git clone https://github.com/formcms/formcms
git clone https://github.com/formcms/formmate
```

### 2. Start Backend (FormCMS)
Run the core CMS with the SQLite demo.
```bash
cd formcms/examples/SqliteDemo
dotnet run
```
_Verify that `http://127.0.0.1:5000` is accessible._

### 3. Configure Environment (FormMate)
Open a new terminal and set up the AI agent with your Gemini API key.
```bash
cd formmate/packages/backend
cp .env.example .env
```
Edit `.env` and add your key (you can get a free one [here](https://aistudio.google.com/app/apikey)):
```ini
GEMINI_API_KEY=your_key_here
```

### 4. Start Development Server
Run the FormMate agent.
```bash
# From formmate root
npm run dev
```
Visit **http://127.0.0.1:5173** to start building!

> **Note:** Please use `127.0.0.1` instead of `localhost` to ensure cookies are shared correctly.

### 💡 Try it out
Once running, try these prompts:
- "Design entities for a library management system"
- "Add sample data for the book entity"
- "Create a query to display all available books"

---

## 🏗️ Architecture

FormCMS is built on a modern, decoupled architecture designed for performance and flexibility.

```mermaid
graph TD
    A[formmate] -->|AI-Generated Schema & UI| B[FormCMS Ecosystem]
    C[FormCmsAdminApp] -->|Management & Editing| D[formcms Backend]
    E[Portal / Frontend] -->|Consumes APIs| D
```

### 1. **formmate** (AI Schema & UI Builder)
The "brain" of the ecosystem. This tool leverages LLMs to architect your data models and design your UI. It translates your natural language requirements into technical configurations that the system understands.

### 2. **formcms** (Backend Engine)
The core high-performance engine built with **ASP.NET Core (C#)**.
- **REST & GraphQL**: Automatically exposes APIs for every entity you define.
- **Normalized Storage**: Optimized for speed (Sqlite, Postgres, SQL Server, MySQL supported).
- **Scale**: Designed to handle millions of records and high-concurrency environments.

### 3. **FormCmsAdminApp** (Management Dashboard)
A sleek, **React-based** administrative interface.
- Manage your entities, queries, and pages.
- Visual editors for relationships and data.
- Built-in audit logging and publication workflows.

---

