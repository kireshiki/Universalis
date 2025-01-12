﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Universalis.Application.ExceptionFilters;

public class TaskCanceledExceptionFilter : IExceptionFilter
{
    private readonly ILogger _logger;

    public TaskCanceledExceptionFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<TaskCanceledExceptionFilter>();
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not TaskCanceledException) return;
        _logger.LogWarning("Request was cancelled");
        context.Result = new StatusCodeResult(504);
        context.ExceptionHandled = true;
        context.HttpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
    }
}
