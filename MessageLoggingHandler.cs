using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Velux.WMS.Web.Handlers
{
    /// <summary>
    /// MessageLoggingHandler handles all api request and responses and add logs base on them. 
    /// </summary>
    public class MessageLoggingHandler : DelegatingHandler
    {
        public const string RegexRemoveNewLineExpression = @"\r\n?|\n";

        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var correlationId = Guid.NewGuid().ToString();

            var loggingInfo = ExtractLoggingInfoFromRequest(request, correlationId);

            var response = base.SendAsync(request, cancellationToken);

            loggingInfo = ExtractResponseLoggingInfo(loggingInfo, response.Result);

            WriteLoggingInfo(loggingInfo);

            return response;
        }

        private ApiLoggingInfo ExtractLoggingInfoFromRequest(HttpRequestMessage request, string correlationId)
        {
            var info = new ApiLoggingInfo
            {
                RequestTime = DateTime.Now.ToLongTimeString(),
                CorrelationId = correlationId,
                HttpMethod = request.Method.Method,
                UriAccessed = request.RequestUri.AbsoluteUri,
                IPAddress = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : "0.0.0.0"
            };

            ExtractMessageHeadersIntoLoggingInfo(info, request.Headers.ToList());

            if (request.Content != null)
            {
                var byteResponse = request.Content.ReadAsByteArrayAsync().Result;
                info.BodyContent = Regex.Replace(System.Text.UTF8Encoding.UTF8.GetString(byteResponse), RegexRemoveNewLineExpression, string.Empty);
            }

            return info;
        }

        private ApiLoggingInfo ExtractResponseLoggingInfo(ApiLoggingInfo loggingInfo, HttpResponseMessage result)
        {
            loggingInfo.ResponseTime = DateTime.Now.ToLongTimeString();

            if (result != null)
            {
                loggingInfo.ResponseCode = ((int)result.StatusCode).ToString();
                loggingInfo.ResponseReasonPhrase = result.ReasonPhrase;
            }

            return loggingInfo;
        }

        private void ExtractMessageHeadersIntoLoggingInfo(ApiLoggingInfo info, List<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            info.RequestHeaders = headers;
        }

        private void WriteLoggingInfo(ApiLoggingInfo requestLoggingInfo)
        {
            Logger.Info(JsonConvert.SerializeObject(requestLoggingInfo));
        }

        /// <summary>
        /// Request/response logging info class.
        /// </summary>
        public sealed class ApiLoggingInfo
        {
            public string CorrelationId { get; set; }
            public string RequestTime { get; set; }
            public string ResponseTime { get; set; }
            public string HttpMethod { get; internal set; }
            public string UriAccessed { get; internal set; }
            public string IPAddress { get; internal set; }
            public string BodyContent { get; internal set; }
            public string ResponseCode { get; internal set; }
            public string ResponseReasonPhrase { get; internal set; }
            public List<KeyValuePair<string, IEnumerable<string>>> RequestHeaders { get; internal set; }
        }
    }
}