# Desktop AI Chat Client

A WinUI3–based chat client built as a learning project and work-in-progress (WIP). This repository is intended for experimentation with Azure OpenAI integration, MVVM architecture, custom Markdown rendering, resilient network patterns, and client-side UX design.

> Note: This project is a personal learning tool and is not affiliated with, endorsed by, or associated with Microsoft, Windows Copilot, GitHub Copilot, or any other Copilot-branded product.

![Alt text](/images/screenshot-prompt-response.png)

---

Table of contents
- [Project Purpose](#project-purpose)
- [Key Features (Engineering-Focused)](#key-features-engineering-focused)
- [Architecture Overview](#architecture-overview)
- [Technologies](#technologies)
- [Getting started](#getting-started)
 - [Prerequisites](#prerequisites)
 - [Environment variables (.env.example)](#environment-variables-envexample)
 - [Run in Visual Studio (recommended)](#run-in-visual-studio-recommended)
 - [Build from CLI (build only)](#build-from-cli-build-only)
- [Known limitations (WIP)](#known-limitations-wip)
- [Roadmap / Next steps](#roadmap--next-steps)
- [Security & Configuration](#security--configuration)
- [License](#license)
- [Contact](#contact)

---

## Project Purpose

This repository exists as a learning and experimentation surface to:

- Explore integrating Azure OpenAI into a real UI application
- Implement a custom Markdown renderer that maps Markdig AST nodes to WinUI elements
- Learn resilient network patterns (retries, backoff, transient detection)
- Practice MVVM architecture, dependency injection, and test-friendly design
- Prototype UX patterns for chat apps (typing indicators, autoscroll, code copy actions)

The project is intentionally a WIP and not production-ready.

---

## Key Features (Engineering-Focused)

- Azure OpenAI chat completion integration (configurable models & prompts)
- Resilient network layer using Polly-style retry patterns
- Local JSON-based conversation persistence with thread-safe write patterns
- Custom `MarkdownBubble` renderer supporting headings, lists, tables, images, fenced/indented code blocks, inline code, and basic HTML spans
- Code blocks include copy-to-clipboard and syntax highlighting integration
- GitHub-style task list detection (`- [x]` / `- [ ]` → disabled checkboxes)
- MVVM structure with observable collections and XAML templates for messages  

#### Markdown Rendering Example  
![Alt text](/images/screenshot-markdown-code.png)

---

## Architecture Overview

```
CopilotClient
│
├── Models/
├── Services/
├── Persistence/
├── Markdown/
├── ViewModels/
└── Views/
```

See the code for service interfaces, retry logic, and the Markdown renderer implementation under `CopilotClient/Controls`.

---

## Technologies

- C#, .NET8, WinUI3 (Windows App SDK)
- Markdig for Markdown parsing
- ColorCode (or equivalent) for syntax highlighting in code blocks
- Azure.AI.OpenAI

---

## Getting started

### Prerequisites

- Windows10/11 (WinUI apps target Windows)
- .NET8 SDK
- Visual Studio2022 or newer with the **Windows App SDK / WinUI** workload installed (recommended)
- Optional: Azure OpenAI access and credentials

### Environment variables (.env.example)

Do not commit secrets. Use environment variables, `dotnet user-secrets`, or your CI secret manager.

```
AOAI_ENDPOINT="https://your-resource.openai.azure.com"
AOAI_KEY="<your-key>"
AOAI_DEPLOYMENT="deployment-name"
AOAI_API_VERSION="2024-03-15-preview"
```

### Run in Visual Studio (recommended for WinUI apps)

1. Open the solution in Visual Studio.
2. In Solution Explorer, right-click the `CopilotClient` project and choose **Set as StartUp Project**.
3. Ensure the correct project is selected in the debug target dropdown and launch the app with F5 or the Start button.

Note: WinUI applications are typically launched from Visual Studio. `dotnet run` may not start the app properly depending on the project template.

### Build from CLI (build only)

To restore and build from a terminal:

```bash
dotnet restore
dotnet build
```

This will produce build artifacts but launching a WinUI app is best done from Visual Studio.

---

## Known limitations (WIP)

- HTML handling is minimal and is not a full sanitizer — treat untrusted input carefully
- Language mapping for syntax highlighting is limited; ColorCode support may require extending
- No CI pipeline or automated tests yet
- Performance and memory for very large conversations are not profiled

---

## Roadmap / Next steps

- Add unit and integration tests and CI validation
- Add theming and better dark/light mode support
- Harden HTML sanitization for untrusted content
- Add profiling and performance optimizations for long-running chats
- Add data storage using EntityFrameworkCore

---

## Security & Configuration

- Never commit API keys or secrets to version control. Use `dotnet user-secrets`, environment variables, or your CI secrets store.
- When rendering remote images or content, consider sanitizing URLs and restricting allowed schemes.
- Review app manifest and capabilities if you add network, file, or device access.

---

## License

This project is provided under the MIT License.

---

*(This README is written to be concise for readers who are evaluating the project as a learning artifact. It is intentionally a work-in-progress.)*
