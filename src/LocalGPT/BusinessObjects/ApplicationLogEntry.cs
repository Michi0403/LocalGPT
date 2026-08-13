using System.ComponentModel.DataAnnotations;

namespace LocalGPT.BusinessObjects
{
    /// <summary>
    /// Represents application log state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
    /// </summary>
    public class ApplicationLogEntry
    {
        /// <summary>
        /// Gets or sets the stable identifier used to identify or correlate this application log instance with related application state.
        /// </summary>
        /// <value>The identifier value exposed by <see cref="ApplicationLogEntry"/>.</value>
        [Key]
        public long Id { get; set; }
        /// <summary>
        /// Gets or sets the timestamp UTC associated with this application log state, using the time semantics implied by the member name.
        /// </summary>
        /// <value>The timestamp UTC value exposed by <see cref="ApplicationLogEntry"/>.</value>
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets the level value that forms part of the application log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The level value exposed by <see cref="ApplicationLogEntry"/>.</value>
        public string Level { get; set; } = "Information";
        /// <summary>
        /// Gets or sets the log level value value that forms part of the application log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The log level value value exposed by <see cref="ApplicationLogEntry"/>.</value>
        public int LogLevelValue { get; set; }
        /// <summary>
        /// Gets or sets the category value that forms part of the application log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The category value exposed by <see cref="ApplicationLogEntry"/>.</value>
        public string Category { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the stable event identifier used to identify or correlate this application log instance with related application state.
        /// </summary>
        /// <value>The event identifier value exposed by <see cref="ApplicationLogEntry"/>.</value>
        public int EventId { get; set; }
        /// <summary>
        /// Gets or sets the event name value that forms part of the application log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The event name value exposed by <see cref="ApplicationLogEntry"/>.</value>
        public string? EventName { get; set; }
        /// <summary>
        /// Gets or sets the message value that forms part of the application log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The message value exposed by <see cref="ApplicationLogEntry"/>.</value>
        public string Message { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the exception value that forms part of the application log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The exception value exposed by <see cref="ApplicationLogEntry"/>.</value>
        public string? Exception { get; set; }
        /// <summary>
        /// Gets or sets the machine name value that forms part of the application log state consumed or produced by the surrounding workflow.
        /// </summary>
        /// <value>The machine name value exposed by <see cref="ApplicationLogEntry"/>.</value>
        public string MachineName { get; set; } = Environment.MachineName;
        /// <summary>
        /// Gets or sets the stable process identifier used to identify or correlate this application log instance with related application state.
        /// </summary>
        /// <value>The process identifier value exposed by <see cref="ApplicationLogEntry"/>.</value>
        public int ProcessId { get; set; } = Environment.ProcessId;
        /// <summary>
        /// Gets or sets the stable thread identifier used to identify or correlate this application log instance with related application state.
        /// </summary>
        /// <value>The thread identifier value exposed by <see cref="ApplicationLogEntry"/>.</value>
        public int ThreadId { get; set; } = Environment.CurrentManagedThreadId;
    }

    /// <summary>
    /// Represents an application log summary application type, grouping the state and behavior that belong to that domain concept.
    /// </summary>
    /// <param name="Id">Identifier of the resource to use for this operation.</param>
    /// <param name="TimestampUtc">Timestamp utc value supplied to the application log summary operation and used when producing its result.</param>
    /// <param name="Level">Level value supplied to the application log summary operation and used when producing its result.</param>
    /// <param name="LogLevelValue">Log level value value supplied to the application log summary operation and used when producing its result.</param>
    /// <param name="Category">Category value supplied to the application log summary operation and used when producing its result.</param>
    /// <param name="EventId">Identifier of the event to use for this operation.</param>
    /// <param name="EventName">Event name value supplied to the application log summary operation and used when producing its result.</param>
    /// <param name="Message">Message value supplied to the application log summary operation and used when producing its result.</param>
    /// <param name="Exception">Exception value supplied to the application log summary operation and used when producing its result.</param>
    public sealed record ApplicationLogSummary(
        long Id,
        DateTime TimestampUtc,
        string Level,
        int LogLevelValue,
        string Category,
        int EventId,
        string? EventName,
        string Message,
        string? Exception);
}
