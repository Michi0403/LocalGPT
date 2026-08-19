namespace LocalGPT.BusinessObjects.Enums
{
    /// <summary>
    /// Defines the supported core log level values used to select or describe behavior in the surrounding workflow.
    /// </summary>
    public enum CoreLogLevel
    {




        /// <summary>
        /// Selects the trace option for <see cref="CoreLogLevel"/>, giving callers a named value for that supported mode or state.
        /// </summary>
        Trace = 0,





        /// <summary>
        /// Selects the debug option for <see cref="CoreLogLevel"/>, giving callers a named value for that supported mode or state.
        /// </summary>
        Debug = 1,




        /// <summary>
        /// Selects the information option for <see cref="CoreLogLevel"/>, giving callers a named value for that supported mode or state.
        /// </summary>
        Information = 2,





        /// <summary>
        /// Selects the warning option for <see cref="CoreLogLevel"/>, giving callers a named value for that supported mode or state.
        /// </summary>
        Warning = 3,





        /// <summary>
        /// Selects the error option for <see cref="CoreLogLevel"/>, giving callers a named value for that supported mode or state.
        /// </summary>
        Error = 4,





        /// <summary>
        /// Selects the critical option for <see cref="CoreLogLevel"/>, giving callers a named value for that supported mode or state.
        /// </summary>
        Critical = 5,




        /// <summary>
        /// Selects the none option for <see cref="CoreLogLevel"/>, giving callers a named value for that supported mode or state.
        /// </summary>
        None = 6,
    }
}
