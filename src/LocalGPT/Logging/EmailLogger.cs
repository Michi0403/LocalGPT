using LocalGPT.BusinessObjects;
using LocalGPT.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;

namespace LocalGPT.Logging;

/// <summary>
/// Represents an email logger application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class EmailLogger : ILogger, IDisposable
{
    /// <summary>
    /// Stores the internal log queue state used by <see cref="EmailLogger"/> while executing its surrounding workflow.
    /// </summary>
    private readonly BlockingCollection<(string Message, string? ExceptionType)> logQueue = new(boundedCapacity: 256);
    /// <summary>
    /// Stores the cancellation source used by <see cref="EmailLogger"/> to stop its current background or asynchronous operation.
    /// </summary>
    private readonly CancellationTokenSource stop = new();
    /// <summary>
    /// Stores the internal config state used by <see cref="EmailLogger"/> while executing its surrounding workflow.
    /// </summary>
    private readonly EmailLoggerCoreOptions config;
    /// <summary>
    /// Stores the internal category name state used by <see cref="EmailLogger"/> while executing its surrounding workflow.
    /// </summary>
    private readonly string categoryName;
    /// <summary>
    /// Stores the internal background task state used by <see cref="EmailLogger"/> while executing its surrounding workflow.
    /// </summary>
    private readonly Task backgroundTask;
    /// <summary>
    /// Stores the internal disposed state used by <see cref="EmailLogger"/> while executing its surrounding workflow.
    /// </summary>
    private int disposed;

    /// <summary>
    /// Initializes a new <see cref="EmailLogger"/> instance and captures the dependencies or initial state required by its email logger workflow.
    /// </summary>
    /// <param name="categoryName">Category name value supplied to the email logger operation and used when producing its result.</param>
    /// <param name="optionsSnapshot">Email logger core options dependency used by the email logger workflow to provide the corresponding application capability.</param>
    public EmailLogger(string categoryName, IOptionsMonitor<EmailLoggerCoreOptions> optionsSnapshot)
    {
        this.categoryName = categoryName;
        config = optionsSnapshot.CurrentValue;
        backgroundTask = Task.Run(ProcessLogQueueAsync);
    }

    /// <summary>
    /// Processes log queue for <see cref="EmailLogger"/>, keeping the operation consistent with the state and invariants of the surrounding email logger workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
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

    /// <summary>
    /// Performs send email for <see cref="EmailLogger"/>, keeping the operation consistent with the state and invariants of the surrounding email logger workflow.
    /// </summary>
    /// <param name="message">Message value supplied to the email logger operation and used when producing its result.</param>
    /// <param name="exceptionType">Exception type value supplied to the email logger operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
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

    /// <summary>
    /// Performs begin scope for <see cref="EmailLogger"/>, keeping the operation consistent with the state and invariants of the surrounding email logger workflow.
    /// </summary>
    /// <typeparam name="TState">Type used for t state values handled by <see cref="EmailLogger"/>.</typeparam>
    /// <param name="state">State value supplied to the email logger operation and used when producing its result.</param>
    /// <returns>The i disposable i logger produced by the operation.</returns>
    IDisposable ILogger.BeginScope<TState>(TState state) =>
        /// <summary>
        /// Runs the disposable scope operation.
        /// </summary>
        new DisposableScope(string.Empty);

    /// <summary>
    /// Determines whether enabled for <see cref="EmailLogger"/>, keeping the operation consistent with the state and invariants of the surrounding email logger workflow.
    /// </summary>
    /// <param name="logLevel">Log level value supplied to the email logger operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool IsEnabled(LogLevel logLevel) =>
        System.Threading.Volatile.Read(ref disposed) == 0 &&
        (int)logLevel >= (int)config.CoreLogLevel;

    /// <summary>
    /// Performs log for <see cref="EmailLogger"/>, keeping the operation consistent with the state and invariants of the surrounding email logger workflow.
    /// </summary>
    /// <typeparam name="TState">Type used for t state values handled by <see cref="EmailLogger"/>.</typeparam>
    /// <param name="logLevel">Log level value supplied to the email logger operation and used when producing its result.</param>
    /// <param name="eventId">Identifier of the event to use for this operation.</param>
    /// <param name="state">State value supplied to the email logger operation and used when producing its result.</param>
    /// <param name="exception">Exception value supplied to the email logger operation and used when producing its result.</param>
    /// <param name="formatter">Formatter value supplied to the email logger operation and used when producing its result.</param>
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

    /// <summary>
    /// Releases resources owned by <see cref="EmailLogger"/> and leaves the email logger workflow in a safely disposed state.
    /// </summary>
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
