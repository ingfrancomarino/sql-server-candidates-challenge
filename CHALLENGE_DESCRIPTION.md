# Challenge Description

## Background

Our company manages multiple clients, each running on-premise SQL Server databases. Data from these client databases must be synchronized with our central cloud platform. This challenge simulates that real-world environment.

You will be working with Microsoft's **AdventureWorks** sample database — a well-known OLTP database that models a fictional bicycle manufacturer. It contains schemas for people, sales, products, purchasing, and more.

Your job is to explore this database, understand its structure and relationships, and build a sync agent that can extract the data our platform needs.

---

## The Real-World Problem

```
┌─────────────────┐         ┌──────────────────────┐
│  Client Machine  │         │   Central Platform    │
│                  │         │                       │
│  ┌────────────┐  │  poll   │  ┌─────────────────┐  │
│  │ Sync Agent │──┼────────>│  │ GET /next-task   │  │
│  │ (your app) │  │         │  └─────────────────┘  │
│  │            │  │  task   │                       │
│  │            │<─┼─────────│  { taskType, params } │
│  │            │  │         │                       │
│  │     ┌──────┤  │         │                       │
│  │     │ SQL  │  │         │                       │
│  │     │Server│  │         │                       │
│  │     └──────┤  │         │                       │
│  │            │  │  result │                       │
│  │            │──┼────────>│  POST /result         │
│  └────────────┘  │         │  { data, status }     │
│                  │         │                       │
└─────────────────┘         └──────────────────────┘
```

Each client runs a SQL Server instance with their business data. A **sync agent** runs on each client's machine as an always-on application. It periodically asks the central platform: *"Do you have any tasks for me?"*

When the platform responds with a task, the agent:
1. Parses the task (what type of data is needed, any filters)
2. Queries the local SQL Server database
3. Formats the results
4. Posts them back to the platform

This is the application you will build.

---

## How It Works

The provided **SyncPlatform** test app simulates the central platform. It runs a small HTTP server with two endpoints:

- **`GET /api/sync/next-task`** — Returns the next pending task, or 204 if the queue is empty
- **`POST /api/sync/result`** — Receives the results of an executed task

The test app has buttons to enqueue different task types. When you click a button, a task is added to the queue. Your sync agent should pick it up, execute it, and post the result.

See [`docs/api-contract.md`](docs/api-contract.md) for the full API specification, and [`docs/sample-payloads/`](docs/sample-payloads/) for example request and response JSON for each task type.

---

## The AdventureWorks Database

[AdventureWorks](https://learn.microsoft.com/en-us/sql/samples/adventureworks-install-configure) is Microsoft's sample OLTP database. It models a company called Adventure Works Cycles — a bicycle manufacturer with sales, products, customers, employees, and more.

The database contains multiple schemas with dozens of tables and relationships between them. **You need to explore the database yourself** to understand its structure and figure out how to extract the data described in the sample payloads.

This is intentional — in the real job, you will encounter unfamiliar client databases and need to understand their structure to build sync queries.

---

## What You're Building

A **.NET always-on application** that:

1. **Connects** to the local AdventureWorks SQL Server database
2. **Polls** the platform API (`GET /api/sync/next-task`) periodically
3. **Executes** the appropriate database query based on the task type
4. **Posts results** back to the platform (`POST /api/sync/result`)

The application type is your choice — console app, Windows Service, Worker Service, or any other approach you find appropriate.

The platform will send four types of sync tasks:
- `GetCustomers`
- `GetProducts`
- `GetOrders`
- `GetProductInventory`

Refer to the sample payloads in [`docs/sample-payloads/`](docs/sample-payloads/) to understand what data each task type expects.

---

## What We Want to See

- **Explain your decisions.** We want to understand what you did and why. Leave comments if necessary, and create meaningful commit messages that explain your reasoning.
- **Fill in [`CHALLENGE_SUBMISSION.md`](CHALLENGE_SUBMISSION.md) before submitting.** Document what you did, your AI usage, and any feedback.
- **Reusable components.** Structure your code so that adding a new sync task type would be straightforward.
- **Server communication.** Your app will ask for next sync tasks to execute, with a known JSON payload that you need to identify, execute, and provide a response.
- **Security.** Implement measures to prevent abuse.
- **Code quality.** SOLID principles. Input validation. Clean, maintainable code.
- **Testing** for important parts of the application.

---

## Time Expectation

This challenge is designed to be completed in approximately **2 hours**.

AI tools are allowed and encouraged — use whatever helps you work effectively. Be transparent about how you used them in your submission.
