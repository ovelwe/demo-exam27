using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DemoExam.Data;
using Microsoft.Data.SqlClient;

namespace DemoExam.Api
{
    public sealed class ApiServer : IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private HttpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _listenTask;

        public bool IsRunning => _listener != null && _listener.IsListening;

        public void Start(string prefix)
        {
            if (IsRunning)
                return;

            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Не указан адрес API.", nameof(prefix));

            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _listener.Start();

            _cancellationTokenSource = new CancellationTokenSource();
            _listenTask = Task.Run(() => ListenLoopAsync(_cancellationTokenSource.Token));
        }

        public void Stop()
        {
            if (_listener == null)
                return;

            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch
            {
                // Сервер уже останавливается.
            }

            try
            {
                _listener.Stop();
                _listener.Close();
            }
            catch
            {
                // При завершении приложения повторное закрытие безопасно игнорируем.
            }

            _listener = null;

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _listenTask = null;
        }

        private async Task ListenLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && IsRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleAsync(context), cancellationToken);
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested || !IsRunning)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private static async Task HandleAsync(HttpListenerContext context)
        {
            try
            {
                var path = (context.Request.Url?.AbsolutePath ?? string.Empty)
                    .TrimEnd('/')
                    .ToLowerInvariant();

                switch (path)
                {
                    case "/api/health":
                        await WriteJsonAsync(context, 200, new { status = "ok" });
                        return;

                    case "/api/customers":
                        await GetCustomersAsync(context);
                        return;

                    case "/api/order-cost":
                        await GetOrderCostAsync(context);
                        return;

                    default:
                        await WriteJsonAsync(context, 404, new { error = "Not found" });
                        return;
                }
            }
            catch (Exception ex)
            {
                try
                {
                    await WriteJsonAsync(context, 500, new { error = ex.Message });
                }
                catch
                {
                    // Если клиент уже закрыл соединение, больше ничего сделать нельзя.
                }
            }
        }

        private static async Task GetCustomersAsync(HttpListenerContext context)
        {
            // Демонстрационная обработка: API не выдает адрес, телефон и ИНН,
            // а возвращает только данные, нужные клиенту.
            // Если в экзаменационном Приложении 2 будет другое правило обработки,
            // изменить нужно только этот метод.
            var table = Database.Query(@"
SELECT CounterpartyId, Name, CounterpartyType
FROM Counterparties
WHERE CounterpartyType = N'Покупатель'
ORDER BY Name;");

            var result = new List<object>();

            foreach (DataRow row in table.Rows)
            {
                result.Add(new
                {
                    id = Convert.ToInt32(row["CounterpartyId"]),
                    name = Convert.ToString(row["Name"]),
                    type = Convert.ToString(row["CounterpartyType"])
                });
            }

            await WriteJsonAsync(context, 200, result);
        }

        private static async Task GetOrderCostAsync(HttpListenerContext context)
        {
            if (!int.TryParse(context.Request.QueryString["orderId"], out var orderId))
            {
                await WriteJsonAsync(context, 400, new { error = "orderId is required" });
                return;
            }

            var table = Database.Query(@"
SELECT OrderId, CustomerName, MaterialCost, OperationCost, TotalCost
FROM dbo.v_OrderProductionCost
WHERE OrderId = @OrderId;",
                new SqlParameter("@OrderId", orderId));

            if (table.Rows.Count == 0)
            {
                await WriteJsonAsync(context, 404, new { error = "Order not found" });
                return;
            }

            var row = table.Rows[0];

            var result = new
            {
                orderId = Convert.ToInt32(row["OrderId"]),
                customer = Convert.ToString(row["CustomerName"]),
                materialCost = Convert.ToDecimal(row["MaterialCost"]),
                operationCost = Convert.ToDecimal(row["OperationCost"]),
                totalCost = Convert.ToDecimal(row["TotalCost"])
            };

            await WriteJsonAsync(context, 200, result);
        }

        private static async Task WriteJsonAsync(HttpListenerContext context, int statusCode, object value)
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            var data = Encoding.UTF8.GetBytes(json);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = data.Length;

            await context.Response.OutputStream.WriteAsync(data, 0, data.Length);
            context.Response.Close();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
