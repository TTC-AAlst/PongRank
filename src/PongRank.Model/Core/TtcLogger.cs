﻿using Serilog;

namespace PongRank.Model.Core;

public class TtcLogger
{
    private readonly ILogger _logger;

    public TtcLogger(Serilog.ILogger logger)
    {
        _logger = logger;
    }

    public void Information(string log)
    {
        _logger.Information(log);
    }

    public void Warning(string log)
    {
        _logger.Warning(log);
    }

    public void Error(string log)
    {
        _logger.Error(log);
    }

    public void Error(Exception exception, string messageTemplate)
    {
        _logger.Error(exception, messageTemplate);
    }

    public void Error(Exception exception, string messageTemplate, object propertyValue)
    {
        _logger.Error(exception, messageTemplate, propertyValue);
    }
}