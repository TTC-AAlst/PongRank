﻿using System.Diagnostics;
using System.Text.Json;
using PongRank.Model.Core;

namespace PongRank.WebApi.Utilities;

public class RequestLoggingFilter
{
    private readonly RequestDelegate _next;
    private readonly TtcLogger _logger;

    public RequestLoggingFilter(RequestDelegate next, TtcLogger logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Path.ToString().StartsWith("/api") || context.Request.Method == HttpMethods.Options)
        {
            await _next(context);
            return;
        }

        var timer = Stopwatch.StartNew();
        context.Request.EnableBuffering();
        var request = context.Request;

        string body = "";
        if (request.Method != HttpMethods.Get && request.Method != HttpMethods.Delete && request.ContentLength > 0)
        {
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
        }

        var qs = request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
        var queryParams = JsonSerializer.Serialize(qs);

        if (qs.Count > 0 && body.Length > 0)
        {
            _logger.Information("{Method} {Path} - Query: {Query}, Body: {Body}", request.Method, request.Path, queryParams, body);
        }
        else if (qs.Count > 0)
        {
            _logger.Information("{Method} {Path} - Query: {Query}", request.Method, request.Path, queryParams);
        }
        else if (body.Length > 0)
        {
            _logger.Information("{Method} {Path} - Body: {Body}", request.Method, request.Path, body);
        }
        else
        {
            _logger.Information("{Method} {Path}", request.Method, request.Path);
        }


        await _next(context);


        _logger.Information("{Method} {Path} - in {Elapsed}", request.Method, request.Path, timer.Elapsed.ToString("g"));
    }
}