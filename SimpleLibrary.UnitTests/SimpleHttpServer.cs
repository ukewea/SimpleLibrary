using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SimpleLibrary.UnitTests
{
    public class SimpleHttpServerFixture : IDisposable
    {
        public SimpleHttpServer? Server { get; private set; }
        public int Port { get; }
        private readonly CancellationTokenSource _cts;

        public SimpleHttpServerFixture(int delayMilliseconds)
        {
            Random random = new Random();
            Port = random.Next(10000, 65536);
            Server = new SimpleHttpServer(port: Port, delayMilliseconds: delayMilliseconds);
            _cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                try
                {
                    await Server.StartAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Server encountered an error: {ex}");
                }
            }, _cts.Token);
        }

        public void Dispose()
        {
            _cts.Cancel();
            Server = null;
        }
    }

    public class SimpleHttpServer
    {
        private readonly TcpListener _listener;
        private readonly int _delayMilliseconds;

        public SimpleHttpServer(int port, int delayMilliseconds)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _delayMilliseconds = delayMilliseconds;
        }

        public async Task StartAsync()
        {
            _listener.Start();
            if (_listener.LocalEndpoint is IPEndPoint ipEndPoint)
            {
                Console.WriteLine($"Server started at http://localhost:{ipEndPoint.Port}");
            }
            else
            {
                Console.WriteLine("Server started, but could not determine the local endpoint.");
            }

            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _ = ProcessClientAsync(client);
            }
        }

        private async Task ProcessClientAsync(TcpClient client)
        {
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                // Read request
                string? line;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                {
                    Console.WriteLine(line);
                }

                // Simulate slow response
                await Task.Delay(_delayMilliseconds);

                // Write response
                string responseBody = "Hello from server!";
                var response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {responseBody.Length}\r\n\r\n{responseBody}";
                await writer.WriteAsync(response);
                await writer.FlushAsync();
            }
        }
    }

}
