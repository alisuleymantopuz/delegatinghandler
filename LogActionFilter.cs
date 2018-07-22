using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace VELUX.VMS.Web.Filters
{
    /// <summary>
    /// LogAttribute handles all api request and responses. It adds logs with detailed info. 
    /// </summary>
    public class LogActionFilter : ActionFilterAttribute
    {
        public const string RegexRemoveNewLineExpression = @"\r\n?|\n";

        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public override Task OnActionExecutingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
        {
            var loggingInfo = ExtractLoggingInfoFromRequest(actionContext);

            WriteLoggingInfo(loggingInfo);

            return base.OnActionExecutingAsync(actionContext, cancellationToken);
        }

        public override Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            var loggingInfo = ExtractLoggingInfoFromResponse(actionExecutedContext);

            WriteLoggingInfo(loggingInfo);

            return base.OnActionExecutedAsync(actionExecutedContext, cancellationToken);
        }
        private ApiRequestLoggingInfo ExtractLoggingInfoFromRequest(HttpActionContext actionContext)
        {
            var info = new ApiRequestLoggingInfo
            {
                RequestTime = DateTime.Now.ToLongTimeString(),
                CorrelationId = actionContext.Request.GetCorrelationId().ToString(),
                HttpMethod = actionContext.Request.Method.Method,
                UriAccessed = actionContext.Request.RequestUri.AbsoluteUri,
                IPAddress = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : "0.0.0.0"
            };

            info.RequestHeaders = actionContext.Request.Headers.ToList();

            if (actionContext.Request.Content != null)
            {
                info.RequestArguments = JsonConvert.SerializeObject(actionContext.ActionArguments);

                info.RequestFormat = actionContext.Request.Content.Headers.ContentType.MediaType;

                var byteResponse = actionContext.Request.Content.ReadAsByteArrayAsync().Result;

                using (var stream = new MemoryStream())
                {
                    var context = (HttpContextBase)actionContext.Request.Properties["MS_HttpContext"];
                    context.Request.InputStream.Seek(0, SeekOrigin.Begin);
                    context.Request.InputStream.CopyTo(stream);
                    string requestBody = Encoding.UTF8.GetString(stream.ToArray());
                    info.BodyContent = Regex.Replace(requestBody, RegexRemoveNewLineExpression, string.Empty);
                }
            }

            return info;
        }

        private ApiResponseLoggingInfo ExtractLoggingInfoFromResponse(HttpActionExecutedContext actionExecutedContext)
        {
            var info = new ApiResponseLoggingInfo
            {
                ResponseTime = DateTime.Now.ToLongTimeString(),
                CorrelationId = actionExecutedContext.Request.GetCorrelationId().ToString()
            };

            if (actionExecutedContext.Response != null)
            {
                info.ResponseCode = ((int)actionExecutedContext.Response.StatusCode).ToString();
                info.ResponseReasonPhrase = actionExecutedContext.Response.ReasonPhrase;
            }

            return info;
        }

        private void WriteLoggingInfo(ApiRequestLoggingInfo requestLoggingInfo)
        {
            Logger.Info(JsonConvert.SerializeObject(requestLoggingInfo, Formatting.Indented));
        }

        private void WriteLoggingInfo(ApiResponseLoggingInfo responseLoggingInfo)
        {
            Logger.Info(JsonConvert.SerializeObject(responseLoggingInfo, Formatting.Indented));
        }
    }

    /// <summary>
    /// Request logging info class.
    /// </summary>
    public sealed class ApiRequestLoggingInfo
    {
        public string CorrelationId { get; set; }
        public string RequestTime { get; set; }
        public string HttpMethod { get; internal set; }
        public string UriAccessed { get; internal set; }
        public string IPAddress { get; internal set; }
        public string RequestFormat { get; internal set; }
        public string RequestArguments { get; internal set; }
        public string BodyContent { get; internal set; }
        public List<KeyValuePair<string, IEnumerable<string>>> RequestHeaders { get; internal set; }
    }

    /// <summary>
    /// Response logging info class.
    /// </summary>
    public sealed class ApiResponseLoggingInfo
    {
        public string CorrelationId { get; set; }
        public string ResponseTime { get; set; }
        public string ResponseCode { get; internal set; }
        public string ResponseReasonPhrase { get; internal set; }
    }
}
