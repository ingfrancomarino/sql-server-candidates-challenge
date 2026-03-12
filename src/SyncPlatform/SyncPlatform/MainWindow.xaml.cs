using System.Windows;
using SyncPlatform.Services;

namespace SyncPlatform;

public partial class MainWindow : Window
{
    private readonly TaskQueueService _taskQueue;
    private readonly HttpSyncServer _server;

    public MainWindow()
    {
        InitializeComponent();

        _taskQueue = new TaskQueueService();
        _server = new HttpSyncServer(_taskQueue);
        _server.OnLog += AppendLog;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _server.Start();
        StatusText.Text = "Server running on http://localhost:5100";
        StatusText.Foreground = System.Windows.Media.Brushes.Green;
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _server.Stop();
    }

    private void BtnCustomers_Click(object sender, RoutedEventArgs e)
    {
        var task = _taskQueue.CreateGetCustomersTask();
        _taskQueue.Enqueue(task);
        AppendLog($"[ENQUEUED] GetCustomers — TaskId: {task.TaskId}");
    }

    private void BtnProducts_Click(object sender, RoutedEventArgs e)
    {
        var task = _taskQueue.CreateGetProductsTask();
        _taskQueue.Enqueue(task);
        AppendLog($"[ENQUEUED] GetProducts — TaskId: {task.TaskId}");
    }

    private void BtnOrders_Click(object sender, RoutedEventArgs e)
    {
        var task = _taskQueue.CreateGetOrdersTask();
        _taskQueue.Enqueue(task);
        AppendLog($"[ENQUEUED] GetOrders — TaskId: {task.TaskId}");
    }

    private void BtnInventory_Click(object sender, RoutedEventArgs e)
    {
        var task = _taskQueue.CreateGetProductInventoryTask();
        _taskQueue.Enqueue(task);
        AppendLog($"[ENQUEUED] GetProductInventory — TaskId: {task.TaskId}");
    }

    private void BtnClearLog_Click(object sender, RoutedEventArgs e)
    {
        LogTextBox.Clear();
    }

    private void AppendLog(string message)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => AppendLog(message));
            return;
        }

        LogTextBox.AppendText(message + Environment.NewLine);
        LogTextBox.ScrollToEnd();
    }
}
