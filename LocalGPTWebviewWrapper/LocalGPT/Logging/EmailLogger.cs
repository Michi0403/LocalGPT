using LocalGPT.BusinessObjects;
using LocalGPT.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;

namespace LocalGPT.Logging;

public sealed class EmailLogger : ILogger, IDisposable
{
    private readonly BlockingCollection<(string Message, string? ExceptionType)> logQueue = new(boundedCapacity: 256);
    private readonly CancellationTokenSource stop = new();
    private readonly EmailLoggerCoreOptions config;
    private readonly string categoryName;
    private readonly Task backgroundTask;
    private int disposed;

    public EmailLogger(string categoryName, IOptionsMonitor<EmailLoggerCoreOptions> optionsSnapshot)
    {
        this.categoryName = categoryName;
        config = optionsSnapshot.CurrentValue;
        backgroundTask = Task.Run(ProcessLogQueueAsync);
    }

    private async Task ProcessLogQueueAsync()
    {
        try
        {
            foreach (var logItem in logQueue.GetConsumingEnumerable(stop.Token))
                await SendEmailAsync(logItem.Message, logItem.ExceptionType, stop.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Normal bounded shutdown.
        }
        catch (ObjectDisposedException)
        {
            // Normal bounded shutdown.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email logger background worker failed: {ex.Message}");
        }
    }

    private async Task SendEmailAsync(
        string message,
        string? exceptionType,
        CancellationToken cancellationToken)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(config.SenderEmail);
            ArgumentException.ThrowIfNullOrWhiteSpace(config.SmtpServer);

            var recipients = config.EmailRecipients
                .Where(address => !string.IsNullOrWhiteSpace(address))
                .ToArray();
            if (recipients.Length == 0)
                throw new InvalidOperationException("At least one email log recipient is required.");

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(config.SenderEmail),
                Subject = $"LocalGPT log: {categoryName}",
                Body = string.IsNullOrWhiteSpace(exceptionType)
                    ? message
                    : $"{message}\nException type: {exceptionType}",
                IsBodyHtml = false
            };

            foreach (var recipient in recipients)
                mailMessage.To.Add(recipient);
            foreach (var cc in config.CcRecipients.Where(address => !string.IsNullOrWhiteSpace(address)))
                mailMessage.CC.Add(cc);
            foreach (var bcc in config.BccRecipients.Where(address => !string.IsNullOrWhiteSpace(address)))
                mailMessage.Bcc.Add(bcc);

            using var smtpClient = new SmtpClient(config.SmtpServer, config.SmtpPort)
            {
                Credentials = new NetworkCredential(config.Username, config.Password),
                EnableSsl = config.EnableSsl
            };

            await smtpClient.SendMailAsync(mailMessage, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal bounded shutdown.
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send log email: {ex.Message}");
        }
    }

    IDisposable ILogger.BeginScope<TState>(TState state) =>
        new DisposableScope(string.Empty);

    public bool IsEnabled(LogLevel logLevel) =>
        Volatile.Read(ref disposed) == 0 &&
        (int)logLevel >= (int)config.CoreLogLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        if (!IsEnabled(logLevel) || logQueue.IsAddingCompleted)
            return;

        var message = $"{DateTime.UtcNow:O} [Category: {categoryName}] [Level: {logLevel}] [EventId: {eventId.Id}] A log event occurred. Review local logs for details.";
        try
        {
            _ = logQueue.TryAdd((message, exception?.GetType().FullName));
        }
        catch (ObjectDisposedException)
        {
            // Logger is shutting down.
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;

        logQueue.CompleteAdding();
        try
        {
            if (!backgroundTask.Wait(TimeSpan.FromSeconds(2)))
            {
                stop.Cancel();
                _ = backgroundTask.Wait(TimeSpan.FromSeconds(2));
            }
        }
        catch (AggregateException ex) when (ex.InnerExceptions.All(inner => inner is OperationCanceledException))
        {
            // Normal bounded shutdown.
        }
        finally
        {
            stop.Cancel();
            stop.Dispose();
            logQueue.Dispose();
        }
    }
}
