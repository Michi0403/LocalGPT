using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>
/// Journals every 1-Wire packet LocalGPT sends or receives: one Information log line with the packet metadata, the
/// decrypted payload at Debug, the raw wire frame at Trace, and a bounded in-memory journal for the live traffic view.
/// Journaling never interrupts a transport; its own failures are only logged.
/// </summary>
public interface IOneWireTrafficJournal
{
    /// <summary>Raised after a packet was journaled or the journal was cleared.</summary>
    event Action? Changed;

    /// <summary>Gets the traffic options currently in effect.</summary>
    /// <value>The live <c>OneWire:Traffic</c> options.</value>
    OneWireTrafficOptions CurrentOptions { get; }

    /// <summary>Captures the plaintext view of an outgoing envelope before it is protected, when payload capture is on.</summary>
    /// <param name="envelope">The envelope about to be protected and sent.</param>
    /// <returns>The plaintext JSON, or empty when payloads are not captured.</returns>
    string CapturePlaintext(OneWireEnvelope envelope);

    /// <summary>Journals one packet: logs it and adds it to the live view.</summary>
    /// <param name="record">The packet as the transport saw it.</param>
    void Record(OneWireTrafficRecord record);

    /// <summary>Returns the journaled packets matching a filter, newest first.</summary>
    /// <param name="query">The filter.</param>
    /// <returns>The matching packets.</returns>
    IReadOnlyList<OneWireTrafficEntry> Query(OneWireTrafficQuery query);

    /// <summary>Returns the total number of packets journaled since LocalGPT started.</summary>
    /// <returns>The total count, including packets no longer in the bounded view.</returns>
    long TotalRecorded();

    /// <summary>Removes every packet from the live view; log files are not affected.</summary>
    void Clear();
}
