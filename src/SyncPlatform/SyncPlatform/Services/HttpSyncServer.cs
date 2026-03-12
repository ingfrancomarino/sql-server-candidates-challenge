using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using SyncPlatform.Models;

namespace SyncPlatform.Services;

public class HttpSyncServer
{
    private readonly TaskQueueService _taskQueue;
    private readonly string _apiKey;
    private readonly int _port;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly List<SyncResult> _receivedResults = new();

    public event Action<string>? OnLog;

    public IReadOnlyList<SyncResult> ReceivedResults => _receivedResults.AsReadOnly();

    public HttpSyncServer(TaskQueueService taskQueue, string apiKey = "candidate-test-key-2026", int port = 5100)
    {
        _taskQueue = taskQueue;
        _apiKey = apiKey;
        _port = port;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");

        try
        {
            _listener.Start();
            Log($"Server started on http://localhost:{_port}");
            _ = ListenAsync(_cts.Token);
        }
        catch (HttpListenerException ex)
        {
            Log($"Failed to start server: {ex.Message}");
            Log($"Try running: netsh http add urlacl url=http://+:{_port}/ user=Everyone");
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();
        Log("Server stopped");
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var context = await _listener!.GetContextAsync();
                _ = Task.Run(() => HandleRequest(context), ct);
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Url?.AbsolutePath ?? "";
        var method = request.HttpMethod;

        Log($"REQUEST {method} {path}");

        try
        {
            // Validate API key
            var apiKey = request.Headers["X-Api-Key"];
            if (apiKey != _apiKey)
            {
                Log("RESPONSE 401 Unauthorized (invalid or missing API key)");
                await WriteJsonResponse(response, 401, new { error = "Unauthorized. Provide a valid X-Api-Key header." });
                return;
            }

            switch (path)
            {
                case "/api/sync/next-task" when method == "GET":
                    await HandleNextTask(response);
                    break;
                case "/api/sync/result" when method == "POST":
                    await HandleResult(request, response);
                    break;
                default:
                    Log($"RESPONSE 404 Not Found");
                    await WriteJsonResponse(response, 404, new { error = $"Unknown endpoint: {method} {path}" });
                    break;
            }
        }
        catch (Exception ex)
        {
            Log($"RESPONSE 500 Internal Server Error: {ex.Message}");
            await WriteJsonResponse(response, 500, new { error = "Internal server error" });
        }
    }

    private async Task HandleNextTask(HttpListenerResponse response)
    {
        if (_taskQueue.TryDequeue(out var task) && task != null)
        {
            var json = JsonSerializer.Serialize(task, new JsonSerializerOptions { WriteIndented = true });
            Log($"RESPONSE 200 OK — Task {task.TaskType} ({task.TaskId})");
            Log($"  Payload: {json}");
            await WriteJsonResponse(response, 200, task);
        }
        else
        {
            Log("RESPONSE 204 No Content (queue empty)");
            response.StatusCode = 204;
            response.Close();
        }
    }

    private async Task HandleResult(HttpListenerRequest request, HttpListenerResponse response)
    {
        using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
        var body = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(body))
        {
            Log("RESPONSE 400 Bad Request (empty body)");
            await WriteJsonResponse(response, 400, new { accepted = false, error = "Request body is empty" });
            return;
        }

        SyncResult? result;
        try
        {
            result = JsonSerializer.Deserialize<SyncResult>(body);
        }
        catch (JsonException ex)
        {
            Log($"RESPONSE 400 Bad Request (invalid JSON: {ex.Message})");
            await WriteJsonResponse(response, 400, new { accepted = false, error = $"Invalid JSON: {ex.Message}" });
            return;
        }

        if (result == null)
        {
            Log("RESPONSE 400 Bad Request (null result)");
            await WriteJsonResponse(response, 400, new { accepted = false, error = "Could not parse result" });
            return;
        }

        // Validate required fields
        if (string.IsNullOrEmpty(result.TaskId))
        {
            Log("RESPONSE 400 Bad Request (missing taskId)");
            await WriteJsonResponse(response, 400, new { accepted = false, error = "Missing required field: taskId" });
            return;
        }

        if (string.IsNullOrEmpty(result.TaskType))
        {
            Log("RESPONSE 400 Bad Request (missing taskType)");
            await WriteJsonResponse(response, 400, new { accepted = false, error = "Missing required field: taskType" });
            return;
        }

        if (result.Status != "completed" && result.Status != "failed")
        {
            Log("RESPONSE 400 Bad Request (invalid status)");
            await WriteJsonResponse(response, 400, new { accepted = false, error = "Status must be 'completed' or 'failed'" });
            return;
        }

        _receivedResults.Add(result);

        var truncatedBody = body.Length > 500 ? body[..500] + "..." : body;
        Log($"RESPONSE 200 OK — Result accepted for {result.TaskType} ({result.TaskId})");
        Log($"  Status: {result.Status}, Records: {result.RecordCount}");
        Log($"  Payload: {truncatedBody}");

        await WriteJsonResponse(response, 200, new { accepted = true, taskId = result.TaskId });
    }

    private static async Task WriteJsonResponse(HttpListenerResponse response, int statusCode, object data)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
        response.Close();
    }

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        OnLog?.Invoke($"[{timestamp}] {message}");
    }
}
