using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace GService
{
    public class HttpRequest
    {
        public HttpRequest() {}
        
        #region Public-Members

        public DateTime TimestampUtc;

        ///// <summary>
        ///// IP address of the requestor (client).
        ///// </summary>
        //public string SourceIp;

        ///// <summary>
        ///// TCP port from which the request originated on the requestor (client).
        ///// </summary>
        //public int SourcePort;

        ///// <summary>
        ///// IP address of the recipient (server).
        ///// </summary>
        //public string DestIp;

        /// <summary>
        /// TCP port on which the request was received by the recipient (server).
        /// </summary>
        public int DestPort;

        /// <summary>
        /// The destination hostname as found in the request line, if present.
        /// </summary>
        public string DestHostname;

        /// <summary>
        /// The destination host port as found in the request line, if present.
        /// </summary>
        public int DestHostPort;

        public HttpMethod Method;

        public string Url;

        public string RawUrl;

        public string RawUrlWithoutQuery;

        public string QueryString;

        public Dictionary<string, string> QueryStringEntries;

        /// <summary>
        /// The number of bytes in the request body.
        /// </summary>
        public long ContentLength;

        /// <summary>
        /// The content type as specified by the requestor (client).
        /// </summary>
        public string ContentType;

        public Dictionary<string, string> Headers;

        public MemoryStream Data;

        #endregion

        #region Constructors-and-Factories
        public HttpRequest(HttpListenerContext ctx)
        {
            #region Check-for-Null-Values

            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (ctx.Request == null) throw new ArgumentNullException(nameof(ctx.Request));

            #endregion

            #region Standard-Request-Items

            TimestampUtc = DateTime.Now.ToUniversalTime();
            Method = (HttpMethod)Enum.Parse(typeof(HttpMethod), ctx.Request.HttpMethod, true);
            Url = ctx.Request.Url.ToString().Trim();
            RawUrl = ctx.Request.RawUrl.ToString().Trim();
            RawUrlWithoutQuery = ctx.Request.RawUrl.ToString().Trim();
            if (RawUrl.Contains("?"))
            {
                RawUrlWithoutQuery = RawUrl.Substring(0, RawUrl.IndexOf("?")).Trim();
                var QueryString = RawUrl.Substring(RawUrl.IndexOf("?") + 1).Trim();

                // Заполнение QueryStringEntries
                QueryStringEntries = new Dictionary<string, string>();

                foreach (var queryKey in ctx.Request.QueryString.AllKeys)
                {
                    // Декодируем значение перед добавлением в словарь
                    QueryStringEntries[queryKey] = Uri.UnescapeDataString(ctx.Request.QueryString[queryKey]);
                }
            }

            // Заполнение Headers
            Headers = new Dictionary<string, string>();
            foreach (var headerKey in ctx.Request.Headers.AllKeys)
            {
                Headers[headerKey] = ctx.Request.Headers[headerKey];
            }

            ContentType = ctx.Request.ContentType;
            ContentLength = ctx.Request.ContentLength64;

            // Извлечение тела запроса
            if (ctx.Request.ContentLength64 > 0)
            {
                Data = new MemoryStream();
                ctx.Request.InputStream.CopyTo(Data);
            }

            #endregion

            try
            {
                Uri _Uri = new Uri(Url);
                DestHostname = _Uri.Host;
                DestHostPort = _Uri.Port;
            }
            catch (Exception)
            {

            }
        }
        #endregion



    






        /* public static string FromTcpClient(TcpClient client)
        {
           if (client == null) throw new ArgumentNullException(nameof(client));

           try
           {
               #region Variables

               StringBuilder requestBuilder = new StringBuilder();
               byte[] lastFourBytes = new byte[4];
               lastFourBytes[0] = 0x00;
               lastFourBytes[1] = 0x00;
               lastFourBytes[2] = 0x00;
               lastFourBytes[3] = 0x00;

               #endregion

               #region Attach-Stream

               NetworkStream stream = client.GetStream();

               if (!stream.CanRead)
               {
                   throw new IOException("Unable to read from stream.");
               }

               #endregion

               #region Read-Headers

               using (MemoryStream headerMs = new MemoryStream())
               {
                   byte[] headerBuffer = new byte[1];
                   int read;

                   // Чтение заголовков
                   while ((read = stream.Read(headerBuffer, 0, headerBuffer.Length)) > 0)
                   {
                       if (read > 0)
                       {
                           // Обновление последних четырех байтов
                           lastFourBytes[0] = lastFourBytes[1];
                           lastFourBytes[1] = lastFourBytes[2];
                           lastFourBytes[2] = lastFourBytes[3];
                           lastFourBytes[3] = headerBuffer[0];

                           // Добавление байта к строке запроса
                           requestBuilder.Append(Encoding.UTF8.GetString(headerBuffer));

                           // Проверка на конец заголовков
                           if ((int)(lastFourBytes[0]) == 13 &&
                               (int)(lastFourBytes[1]) == 10 &&
                               (int)(lastFourBytes[2]) == 13 &&
                               (int)(lastFourBytes[3]) == 10)
                           {
                               break;
                           }
                       }
                   }
               }

               #endregion

               #region Read-Data

               // Чтение тела запроса, если оно есть
               string[] requestLines = requestBuilder.ToString().Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

               if (requestLines.Length > 0)
               {
                   string requestLine = requestLines[0]; // Первая строка - это метод, URL и версия
                   string[] parts = requestLine.Split(' '); // Разделяем на части

                   if (parts.Length >= 3 && int.TryParse(parts[^1], out int contentLength)) // Проверка на наличие Content-Length
                   {
                       byte[] data = new byte[contentLength];

                       using (MemoryStream dataMs = new MemoryStream())
                       {
                           int bytesRead;
                           while ((bytesRead = stream.Read(data, 0, data.Length)) > 0)
                           {
                               dataMs.Write(data, 0, bytesRead);
                               if (dataMs.Length >= contentLength) break; // Прерываем, если прочитано достаточно данных
                           }

                           // Добавляем тело запроса к строке запроса
                           requestBuilder.Append(Encoding.UTF8.GetString(dataMs.ToArray()));
                       }
                   }
               }

               #endregion

               return requestBuilder.ToString(); // Возвращаем весь запрос как строку
           }
           catch (Exception ex)
           {
               throw new IOException("Error reading from TcpClient.", ex);
           }
        } */

    }
}