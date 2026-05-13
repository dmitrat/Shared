namespace OutWit.Shared.Email.Plugin.Null
{
    /// <summary>
    /// Behaviour mode for the Null email transport.
    /// </summary>
    public enum NullEmailMode
    {
        /// <summary>
        /// Pretend success: write the recipient, subject and first line of the body
        /// to the host's log at <c>Warning</c> level, then return a successful result.
        /// Useful for dev / staging / first-deploy walkthroughs where an operator
        /// copies a verification link out of the logs.
        /// </summary>
        LogOnly,

        /// <summary>
        /// Fail fast: log an error and return an
        /// <c>OutWit.Common.Email.EmailFailureKind.Permanent</c> failure. Useful for
        /// production deployments that genuinely don't need outbound email — flows
        /// that depend on email surface the failure cleanly instead of silently
        /// believing mail was sent.
        /// </summary>
        Drop
    }
}
