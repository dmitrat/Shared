namespace OutWit.Shared.Messenger.Provider.Null
{
    /// <summary>
    /// Behaviour mode for the Null messenger transport.
    /// </summary>
    public enum NullMessengerMode
    {
        /// <summary>
        /// Pretend success: write the target and first line of the text to the host's
        /// log at <c>Warning</c> level, then return a successful result. Useful for
        /// dev / staging where no messenger is wired up yet.
        /// </summary>
        LogOnly,

        /// <summary>
        /// Fail fast: log an error and return a
        /// <c>OutWit.Common.Messenger.MessengerFailureKind.Permanent</c> failure.
        /// Useful for deployments that genuinely don't need messenger notifications —
        /// flows that depend on them surface the failure cleanly.
        /// </summary>
        Drop
    }
}
