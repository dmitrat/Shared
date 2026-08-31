using System.Security.Cryptography;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Shared.Secrets.Providers
{
    /// <summary>
    /// The answer to a read. Carries the secret only when <see cref="Status"/> is
    /// <see cref="SecretStatus.Found"/>. <see cref="Secret"/> is deliberately excluded from
    /// <see cref="object.ToString"/>: a result that reaches a log line must never carry the payload.
    /// </summary>
    public sealed class SecretResult : ModelBase
    {
        #region Functions

        /// <summary>
        /// Value comparison. Secret bytes are compared with
        /// <see cref="CryptographicOperations.FixedTimeEquals(System.ReadOnlySpan{byte}, System.ReadOnlySpan{byte})"/>
        /// rather than an early-exit loop.
        /// </summary>
        /// <param name="modelBase">The model to compare with.</param>
        /// <param name="tolerance">Unused for this model.</param>
        /// <returns>True when status, message and secret bytes all match.</returns>
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not SecretResult other)
                return false;

            return Status == other.Status
                   && Message.Is(other.Message)
                   && SecretsEqual(Secret, other.Secret);
        }

        /// <summary>
        /// Creates a deep copy; the secret bytes are copied, not shared.
        /// </summary>
        /// <returns>A new <see cref="SecretResult"/> with the same values.</returns>
        public override ModelBase Clone()
        {
            return new SecretResult
            {
                Status = Status,
                Secret = (byte[]?)Secret?.Clone(),
                Message = Message
            };
        }

        private static bool SecretsEqual(byte[]? left, byte[]? right)
        {
            if (left == null || right == null)
                return left == right;

            return CryptographicOperations.FixedTimeEquals(left, right);
        }

        #endregion

        #region Properties

        /// <summary>
        /// What the store answered — see <see cref="SecretStatus"/>.
        /// </summary>
        [ToString]
        public SecretStatus Status { get; init; }

        /// <summary>
        /// The secret, only when <see cref="Status"/> is <see cref="SecretStatus.Found"/>.
        /// The caller owns this buffer and should clear it with
        /// <see cref="CryptographicOperations.ZeroMemory"/> when done. Never logged.
        /// </summary>
        public byte[]? Secret { get; init; }

        /// <summary>
        /// What went wrong, in words a support engineer can act on. Never contains the secret.
        /// </summary>
        [ToString]
        public string? Message { get; init; }

        #endregion
    }
}
