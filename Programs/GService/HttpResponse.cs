using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
 
namespace GService
{
    /// <summary>
    /// Response to an HTTP request.
    /// </summary>
    public class HttpResponse
    { 
        public HttpResponse(){}


        /*
        #region Public-Members

        public DateTime TimestampUtc;

        public string ProtocolVersion;

        //
        // Response values
        //

        public int StatusCode;

        public string StatusDescription;
         
        public Dictionary<string, string> Headers;

        public string ContentType;

        public long ContentLength;

        public byte[] Data;

        public Stream DataStream;

        //public string DataMd5;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Create an uninitialized HttpResponse object.
        /// </summary>
        public HttpResponse()
        {
            TimestampUtc = DateTime.Now.ToUniversalTime();
            Headers = new Dictionary<string, string>();
        }

        /// <summary>
        /// Create a new HttpResponse object with no data.
        /// </summary>
        /// <param name="req">The HttpRequest object for which this request is being created.</param>
        /// <param name="status">The HTTP status code to return to the requestor (client).</param>
        /// <param name="headers">User-supplied headers to include in the response.</param>
        public HttpResponse(HttpRequest req, int status, Dictionary<string, string> headers)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            SetBaseVariables(req, status, headers, null);
            SetStatusDescription();

            DataStream = null;
            Data = null;
            ContentLength = 0;
        }

        /// <summary>
        /// Create a new HttpResponse object.
        /// </summary>
        /// <param name="req">The HttpRequest object for which this request is being created.</param>
        /// <param name="status">The HTTP status code to return to the requestor (client).</param>
        /// <param name="headers">User-supplied headers to include in the response.</param>
        /// <param name="contentType">User-supplied content-type to include in the response.</param>
        /// <param name="data">The data to return to the requestor in the response body.</param> 
        public HttpResponse(HttpRequest req, int status, Dictionary<string, string> headers, string contentType, byte[] data)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            SetBaseVariables(req, status, headers, contentType);
            SetStatusDescription();

            DataStream = null;
            Data = data;
            if (Data != null && Data.Length > 0) ContentLength = Data.Length;
        }

        /// <summary>
        /// Create a new HttpResponse object.
        /// </summary>
        /// <param name="req">The HttpRequest object for which this request is being created.</param>
        /// <param name="status">The HTTP status code to return to the requestor (client).</param>
        /// <param name="headers">User-supplied headers to include in the response.</param>
        /// <param name="contentType">User-supplied content-type to include in the response.</param>
        /// <param name="data">The data to return to the requestor in the response body.</param> 
        public HttpResponse(HttpRequest req, int status, Dictionary<string, string> headers, string contentType, string data)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            SetBaseVariables(req, status, headers, contentType);
            SetStatusDescription();

            DataStream = null;
            Data = null;
            if (!String.IsNullOrEmpty(data)) Data = Encoding.UTF8.GetBytes(data);
            if (Data != null && Data.Length > 0) ContentLength = Data.Length;
        }

        /// <summary>
        /// Create a new HttpResponse object.
        /// </summary>
        /// <param name="req">The HttpRequest object for which this request is being created.</param>
        /// <param name="status">The HTTP status code to return to the requestor (client).</param>
        /// <param name="headers">User-supplied headers to include in the response.</param>
        /// <param name="contentType">User-supplied content-type to include in the response.</param>
        /// <param name="contentLength">The number of bytes the client should expect to read from the data stream.</param>
        /// <param name="dataStream">The stream containing the data that should be read to return to the requestor.</param>
        public HttpResponse(HttpRequest req, int status, Dictionary<string, string> headers, string contentType, long contentLength, Stream dataStream)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (contentLength < 0) throw new ArgumentException("Content length must be zero or greater.");
            SetBaseVariables(req, status, headers, contentType);
            SetStatusDescription();

            Data = null;
            DataStream = dataStream;
            ContentLength = contentLength; 

            if (contentLength > 0 && !DataStream.CanRead) throw new IOException("Cannot read from input stream.");
            if (contentLength > 0 && !DataStream.CanSeek) throw new IOException("Cannot perform seek on input stream.");
            if (contentLength > 0) DataStream.Seek(0, SeekOrigin.Begin);
        }

        #endregion

        #region Private-Methods

        private void SetBaseVariables(HttpRequest req, int status, Dictionary<string, string> headers, string contentType)
        {
            TimestampUtc = req.TimestampUtc;
             
            Headers = headers;
            ContentType = contentType; 
            if (String.IsNullOrEmpty(ContentType)) ContentType = "application/octet-stream";

            StatusCode = status; 
        }

        private void SetStatusDescription()
        { 
            switch (StatusCode)
            {
                case 200:
                    StatusDescription = "OK";
                    break;

                case 201:
                    StatusDescription = "Created";
                    break;

                case 204:
                    StatusDescription = "No Content";
                    break;

                case 301:
                    StatusDescription = "Moved Permanently";
                    break;

                case 302:
                    StatusDescription = "Moved Temporarily";
                    break;

                case 304:
                    StatusDescription = "Not Modified";
                    break;

                case 400:
                    StatusDescription = "Bad Request";
                    break;

                case 401:
                    StatusDescription = "Unauthorized";
                    break;

                case 403:
                    StatusDescription = "Forbidden";
                    break;

                case 404:
                    StatusDescription = "Not Found";
                    break;

                case 405:
                    StatusDescription = "Method Not Allowed";
                    break;

                case 429:
                    StatusDescription = "Too Many Requests";
                    break;

                case 500:
                    StatusDescription = "Internal Server Error";
                    break;

                case 501:
                    StatusDescription = "Not Implemented";
                    break;

                case 503:
                    StatusDescription = "Service Unavailable";
                    break;

                default:
                    StatusDescription = "Unknown";
                    return;
            } 
        }

        private byte[] AppendBytes(byte[] orig, byte[] append)
        {
            if (append == null) return orig;
            if (orig == null) return append;

            byte[] ret = new byte[orig.Length + append.Length];
            Buffer.BlockCopy(orig, 0, ret, 0, orig.Length);
            Buffer.BlockCopy(append, 0, ret, orig.Length, append.Length);
            return ret;
        }

        #endregion
        */
    }
}
