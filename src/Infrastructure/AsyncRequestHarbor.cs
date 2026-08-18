using Microsoft.EntityFrameworkCore.Storage.Json;
using WarHack.Infrastructure.Models;

namespace WarHack.Infrastructure;

public class AsyncRequestHost : DelegatingHandler
{
    private static CancellationToken cancellationToken;
    private readonly ILogger<ILogShipHarbor> logger;

/*
    public static async Task<HttpResponseData<List<T>>> SendRequestForListAsync<T>(HttpRequestMessage request, HttpClient client, string exceptionClientName)
    {
        const string _functionName = "SendRequestForListAsync<T>()";
        try
        {
                throwIfCancellationRequested();
            //var response = await SendAsync(request, cancellationToken);
            var response = await ExecuteSendRequestAsync(request, client);
            if (response.IsSuccessStatusCode)
            {
                List<T> jsonResponse = new List<T>();
                //JsonConvert.PopulateObject(response.Content.ReadAsStringAsync().Result, jsonResponse);
                //with constructor and inheritance
                //var repoResponse = new HttpResponseData<List<T>>(true, response.Content.ReadAsStringAsync().Result, jsonResponse );
                //without:
                return new HttpResponseData<List<T>>(response.IsSuccessStatusCode, response?.RequestMessage?.ToString() ?? "farts", jsonResponse);
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"{_functionName}: Request failed - {client.BaseAddress}/{request.RequestUri}  \r\n { _className}: {response.RequestMessage}");
                //ogg($"{_functionName}: Request failed - {client.BaseAddress}/{request.RequestUri} \r\n { _className}: {response.RequestMessage}");
                List<T> jsonResponse = new List<T>();
                //var repoResponse = new HttpResponseData<List<T>>(true, response.Content.ReadAsStringAsync().Result, jsonResponse);
                return new HttpResponseData<List<T>>(response.IsSuccessStatusCode, response?.RequestMessage?.ToString() ?? "farts", jsonResponse);

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in {_className} - {_functionName}. {exceptionClientName}. Exception: {ex.Message}");
            //LoggerLQ.LogQueue($"Exception in {_className} - {_functionName}. {exceptionClientName}. Exception: {ex.Message}");
            List<T> jsonResponse = new List<T>();
            //var repoResponse = new HttpResponseData<List<T>>(true, null, jsonResponse);
            return new HttpResponseData<List<T>>(false, "Request failed", jsonResponse);

        }
    }

    public static async Task<HttpResponseData<T>> SendRequestAsync<T>(HttpRequestMessage request, HttpClient client, string exceptionClientName) where T : new()
    {
        const string _functionName = "SendRequestAsync<T>()";
        try
        {
            var response = await ExecuteSendRequestAsync(request, client);
            if (response.IsSuccessStatusCode)
            {
                T jsonResponse = new();
                //List<T> jrList = new List<T>();
                // var settings = new JsonSerializerSettings
                // {
                //     DateParseHandling = DateParseHandling.DateTimeOffset,
                //     NullValueHandling = NullValueHandling.Ignore,
                // };
                // var jRslt = await response.Content.ReadAsStringAsync();
                // //if (jsonResponse.GetType() == typeof(BadgeIntegration.Models.EdFiV3Student))
                // if (jRslt[0] == '[')
                // {
                //     //var dobj = JsonConvert.DeserializeObject<T[]>(jRslt);
                //     var y = dobj.First();
                //     //var szObj = JsonConvert.SerializeObject(y);
                //     //JsonConvert.PopulateObject(szObj, jsonResponse, settings);
                // }
                // else
                // {
                //     //JsonConvert.PopulateObject(jRslt, jsonResponse);
                // }
                //JsonConvert.PopulateObject(jRslt, jsonResponse);
                //JsonConvert.PopulateObject(jRslt, jrList);
                //return new HttpResponseData<T> { IsSuccess = true, ResponseContent = jrList[0] != null ? jrList[0] : jrList[1] };
                return new HttpResponseData<T> { Content = }
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"{_functionName}: Request failed - {client.BaseAddress}/{request.RequestUri}  \r\n { _className}: {response.RequestMessage}");
                LoggerLQ.LogQueue($"{_functionName}: Request failed - {client.BaseAddress}/{request.RequestUri}  \r\n { _className}: {response.RequestMessage}");
                return new HttpResponseData<T> { IsSuccess = true, StatusCode = HttpStatusCode.BadRequest };
            }
        }
        catch (Exception exc)
        {
            LoggerLQ.LogQueue($"Exception in {_className} at {_functionName}. Exception: {exc.Message}");
            Console.WriteLine($"Exception in {_className} at {_functionName}. Exception: {exc.Message}");
            return new HttpResponseData<T> { IsSuccess = true, StatusCode = HttpStatusCode.BadRequest };
        }
    }

    public static async Task<ServerResponse> SendPropagateRequestAsync(HttpRequestMessage request, HttpClient client, string exceptionClientName)
    {
        const string _functionName = "SendPropagateRequestAsync()";
        try
        {
            var response = await ExecuteSendRequestAsync(request, client);
            return new ServerResponse
            {
                HttpRespMsg = response,
                Message = await response.Content.ReadAsStringAsync()
            };
        }
        catch (Exception exc)
        {
            Console.WriteLine($"Exception in {_className} at {_functionName}. Exception: {exc.Message}");
            LoggerLQ.LogQueue($"Exception in {_className} at {_functionName}. Exception: {exc.Message}");
            return new ServerResponse
            {
                HttpRespMsg = new HttpResponseMessage(HttpStatusCode.BadRequest),
                Message = $"Exception in {_className} at {_functionName}. Exception: {exc.Message}"
            };
        }
    }

    private static async Task<HttpResponseMessage> ExecuteSendRequestAsync(HttpRequestMessage request, HttpClient client)
    {
        const string _fName = nameof(ExecuteSendRequestAsync);
        try
        {
            var response = await client.SendAsync(request);
            return response;
        }
        catch (ArgumentNullException exc)
        {
            Console.WriteLine($"{_fName}-{exc.TargetSite}:{exc.Message}");
        }
        catch (InvalidOperationException exc)
        {
            Console.WriteLine($"{_fName}-{exc.TargetSite}:{exc.Message}");
        }
        catch (HttpRequestException exc)
        {
            Console.WriteLine($"{_fName}-{exc.TargetSite}:{exc.Message}");
        }
        catch (TaskCanceledException exc)
        {
            Console.WriteLine($"{_fName}-{exc.TargetSite}:{exc.Message}");
        }
        catch (Exception exc)
        {
            Console.WriteLine($"{_fName}-{exc.TargetSite}:{exc.Message}");
        }
        return new HttpResponseMessage();
    }

    public static HttpRequestMessage GetNewRequestMessage(HttpRequestMessage request)
    {
        HttpRequestMessage newRequest = new HttpRequestMessage(request.Method, request.RequestUri);

        if (request.Content != null)
        {
            newRequest.Content = request.Content;
        }
        if (request.Properties != null)
        {
            foreach (var prop in request.Properties)
                newRequest.Properties.Add(prop.Key, prop.Value);
        }
        return newRequest;
    }
    */
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage reqMsg, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        cancellationToken.ThrowIfCancellationRequested();
        //var policy = Policy
        var response = await base.SendAsync(reqMsg, cancellationToken);
        Console.WriteLine($"Completed request in {sw.ElapsedMilliseconds}ms.");
        return response;
    }
}