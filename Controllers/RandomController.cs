using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace LinuxCore.Controllers
{
    [Route("api/[controller]")]
    public class RandomController : Controller
    {
        private Random mRandGen = new Random();

        // GET: api/<controller>
        [HttpGet]
        public async Task Get()
        {
            var context = ControllerContext.HttpContext;
            var isSocketRequest = context.WebSockets.IsWebSocketRequest;

            if (isSocketRequest)
            {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await ListenToSocket(webSocket);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }


        private async Task ListenToSocket(WebSocket socket)
        {
            Debug.WriteLine("client connected");
            // Start streaming data
            CancellationTokenSource source = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem((object t) => StreamRandomData(socket, (CancellationToken)t), source.Token);

            // Listen to the socket for message or close
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                string message = Encoding.UTF8.GetString(buffer);
                Debug.WriteLine($"received: {message}");
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            source.Cancel();
            await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            Debug.WriteLine("client disconnected");
        }

        private void StreamRandomData(WebSocket socket, CancellationToken token)
        {
            var dataDate = new DateTime();
            var openValue = 100F;

            while(!token.IsCancellationRequested)
            {
                var newData = generateData(dataDate, openValue);
                var message = new SocketMessage<FinancialData> {
                    Type = "data",
                    Content = newData
                };

                var messageStr = JsonConvert.SerializeObject(message);
                socket.SendAsync(Encoding.UTF8.GetBytes(messageStr), WebSocketMessageType.Text, true, CancellationToken.None);

                var delayMS = mRandGen.Next(100);
                dataDate = dataDate.AddMilliseconds(delayMS);
                openValue = newData.Close;

                Thread.Sleep(delayMS);
            }
        }

        private FinancialData generateData(DateTime date, float openValue)
        {
            var closeValue = openValue + (float)Math.Log(mRandGen.NextDouble() * 50) - 2.8F;
            if (closeValue < 10) closeValue = 10;

            return new FinancialData {
                Date = date,
                Open = openValue,
                Close = closeValue,
                High = Math.Max(openValue, closeValue) + (float)mRandGen.NextDouble() * 2,
                Low = Math.Min(openValue, closeValue) - (float)mRandGen.NextDouble() * 2
            };
        }
    }

    public class SocketMessage<T>
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("content")]
        public T Content { get; set; }
    }

    public class FinancialData
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("open")]
        public float Open { get; set; }

        [JsonProperty("close")]
        public float Close { get; set; }

        [JsonProperty("high")]
        public float High { get; set; }

        [JsonProperty("low")]
        public float Low { get; set; }
    }
}
