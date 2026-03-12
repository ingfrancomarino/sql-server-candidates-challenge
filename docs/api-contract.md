# API Contract

The SyncPlatform test app runs an HTTP server on `http://localhost:5100`. Your sync agent communicates with it using these two endpoints.

## Authentication

All requests must include the API key header:

```
X-Api-Key: candidate-test-key-2026
```

Requests without a valid API key receive `401 Unauthorized`.

---

## GET /api/sync/next-task

Retrieves the next pending sync task from the queue.

### Response: 200 OK (task available)

```json
{
  "taskId": "01JQFG8N3XRTV5KHW2YP4M7B6C",
  "taskType": "GetCustomers",
  "parameters": {
    "modifiedSince": "2026-01-01T00:00:00Z"
  },
  "createdAt": "2026-03-12T10:30:00Z"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `taskId` | string (ULID) | Unique identifier for this task |
| `taskType` | string | One of: `GetCustomers`, `GetProducts`, `GetOrders`, `GetProductInventory` |
| `parameters` | object | Task-specific parameters |
| `parameters.modifiedSince` | string (ISO 8601) | Only return records modified on or after this date |
| `createdAt` | string (ISO 8601) | When the task was created |

### Response: 204 No Content

No tasks are currently queued. The response body is empty.

---

## POST /api/sync/result

Submit the result of an executed sync task.

### Request Body

```json
{
  "taskId": "01JQFG8N3XRTV5KHW2YP4M7B6C",
  "taskType": "GetCustomers",
  "status": "completed",
  "data": [ ... ],
  "recordCount": 42,
  "executedAt": "2026-03-12T10:30:05Z",
  "errorMessage": null
}
```

| Field | Type | Description |
|-------|------|-------------|
| `taskId` | string (ULID) | Must match the task that was retrieved |
| `taskType` | string | Must match the task type |
| `status` | string | `"completed"` or `"failed"` |
| `data` | array or null | The query results (null if failed) |
| `recordCount` | integer | Number of records in data |
| `executedAt` | string (ISO 8601) | When the query was executed |
| `errorMessage` | string or null | Error description if status is `"failed"` |

### Response: 200 OK

```json
{
  "accepted": true,
  "taskId": "01JQFG8N3XRTV5KHW2YP4M7B6C"
}
```

### Response: 400 Bad Request

Returned when required fields are missing or invalid.

```json
{
  "accepted": false,
  "error": "Missing required field: taskId"
}
```

---

## Task Types

The platform can request four types of sync tasks. See `docs/sample-payloads/` for example request and response payloads for each type:

- `GetCustomers`
- `GetProducts`
- `GetOrders`
- `GetProductInventory`
