# Scroll Guardian — Production-Ready SaaS

> **"Make the next 10 minutes useful."**  
> Scroll Guardian analyzes short-form content consumption across Instagram Reels, YouTube Shorts, and TikTok to help users transform passive digital scrolling into intentional learning and verified cognitive retention.

---

## Architecture Overview

```
                                  ┌─────────────────────────────────────────┐
                                  │      Browser Extension (Manifest V3)    │
                                  │  Instagram Reels / YouTube Shorts / HUD │
                                  └────────────────────┬────────────────────┘
                                                       │
                                  ┌────────────────────┴────────────────────┐
                                  │      React + TypeScript Frontend        │
                                  │   Tailwind CSS Design System + Modals   │
                                  └────────────────────┬────────────────────┘
                                                       │
                                                       ▼
                                  ┌─────────────────────────────────────────┐
                                  │    ASP.NET Core 8 Web API (Clean Arch)  │
                                  │  JWT Auth + Rate Limiting + Serilog     │
                                  └────────────────────┬────────────────────┘
                                                       │
         ┌─────────────────────────────────────────────┼─────────────────────────────────────────────┐
         │                                             │                                             │
         ▼                                             ▼                                             ▼
┌──────────────────┐                         ┌───────────────────┐                         ┌───────────────────┐
│ AI Ingestion Q   │                         │ PostgreSQL & EF   │                         │ Retention Engine  │
│ Bounded Channels │                         │ Core Relational   │                         │ Spaced Recall     │
│ Gemini / Heuristic│                        │ Database + Index  │                         │ Taxonomy Map      │
└──────────────────┘                         └───────────────────┘                         └───────────────────┘
```

---

## Core Product Capabilities

1. **Autonomous Telemetry & Intentional Interventions**: Non-intrusive floating HUD overlay on Reels and Shorts monitoring session length against configured aggressiveness thresholds (*Gentle: 45m*, *Balanced: 30m*, *Proactive: 15m*).
2. **AI Semantic Content Analysis**: Strictly validated JSON classification extracting informational depth (0-100), educational value, goal relevance, and dynamically generated spaced recall questions.
3. **Knowledge Retention & Taxonomy Map**: Measures verified retention rates based strictly on completed user quizzes—zero fake data or placeholder metrics.
4. **"Find Something Useful" Hub**: Instant one-click recommendations matching weak knowledge nodes and active personal goals with transparent reasoning.
5. **Complete Data Governance & Privacy**: GDPR/CCPA compliant JSON telemetry export, individual item deletion, history purging, and instant account deletion.
6. **Billing & Subscriptions**: Free and Pro tiers with quota enforcement and Stripe subscription architecture.

---

## Relational Database Entities (PostgreSQL / EF Core)

* **Identity & Governance**: `User`, `RefreshToken`, `UserGoal`, `UserPreference`, `PrivacyConsent`, `Device`, `AuditLog`, `Subscription`
* **Content Ingestion**: `ContentItem`, `ContentAnalysis`, `ContentSession`, `ContentEvent`, `ContentCategory`, `ContentTopic`, `UserTopic`
* **Cognitive Retention**: `KnowledgeCheck`, `KnowledgeQuestion`, `KnowledgeAnswer`, `LearningProgress`
* **Engagement & Analytics**: `Intervention`, `Recommendation`, `RecommendationInteraction`, `DailySummary`, `WeeklySummary`

---

## Getting Started

### 1. Backend API (.NET 8)

```bash
cd backend
dotnet restore
dotnet build
dotnet test
dotnet run --project src/ScrollGuardian.Api
```

The API starts at `http://localhost:5000` with interactive Swagger UI at `http://localhost:5000/swagger`.

### 2. Frontend Application (React + Vite + TypeScript)

```bash
cd frontend
npm install
npm run dev
```

The application will run on `http://localhost:5173` and automatically proxies `/api` calls to `http://localhost:5000`.

### 3. Browser Extension (Chrome / Edge / Brave / Firefox)

1. Open `chrome://extensions` in your browser.
2. Enable **Developer mode** (top right toggle).
3. Click **Load unpacked** and select the `extension` folder inside this repository.
4. Open Instagram Reels or YouTube Shorts—the Scroll Guardian HUD will appear in the bottom-right corner.

---

## Docker Production Deployment

```bash
# Start PostgreSQL, API, and Frontend with Docker Compose
docker-compose up --build -d
```

* **Frontend**: `http://localhost:80`
* **API**: `http://localhost:5000`
* **PostgreSQL**: `localhost:5432`

---

## Test Suite Execution

```bash
dotnet test backend/ScrollGuardian.sln
```

All 13 unit tests and integration tests verify:
* Registration, Login, and Refresh Token lifecycle
* Rule-based & Gemini NLP content categorization and quiz generation
* Intervention session duration thresholds and snooze handling
* True mathematical retention accuracy and topic mastery updates
* End-to-end API user journeys with `WebApplicationFactory`

---

## License & Compliance

Built for user autonomy and digital habit intentionality without manipulative dark patterns.
