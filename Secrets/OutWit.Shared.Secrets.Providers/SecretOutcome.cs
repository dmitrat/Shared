using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Shared.Secrets.Providers
{
    /// <summary>
    /// The answer to a store or delete: <see cref="SecretResult"/> without the payload.
    /// The status reports the state the store is now in — a successful store is
    /// <see cref="SecretStatus.Found"/>, a successful delete is <see cref="SecretStatus.NotFound"/>.
    /// </summary>
    public sealed class SecretOutcome : ModelBase
    {
        #region Functions

        /// <summary>
        /// Value comparison.
        /// </summary>
        /// <param name="modelBase">The model to compare with.</param>
        /// <param name="tolerance">Unused for this model.</param>
        /// <returns>True when status and message match.</returns>
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not SecretOutcome other)
                return false;

            return Status == other.Status && Message.Is(other.Message);
        }

        /// <summary>
        /// Creates a copy.
        /// </summary>
        /// <returns>A new <see cref="SecretOutcome"/> with the same values.</returns>
        public override ModelBase Clone()
        {
            return new SecretOutcome
            {
                Status = Status,
                Message = Message
            };
        }

        /// <summary>
        /// True when the operation succeeded — the status is <see cref="SecretStatus.Found"/>
        /// after a store or <see cref="SecretStatus.NotFound"/> after a delete.
        /// </summary>
        /// <returns>Whether the operation left the store in the requested state.</returns>
        public bool IsSuccess()
        {
            return Status == SecretStatus.Found || Status == SecretStatus.NotFound;
        }

        #endregion

        #region Properties

        /// <summary>
        /// What the store answered — see <see cref="SecretStatus"/>.
        /// </summary>
        [ToString]
        public SecretStatus Status { get; init; }

        /// <summary>
        /// What went wrong, in words a support engineer can act on. Never contains the secret.
        /// </summary>
        [ToString]
        public string? Message { get; init; }

        #endregion
    }
}
