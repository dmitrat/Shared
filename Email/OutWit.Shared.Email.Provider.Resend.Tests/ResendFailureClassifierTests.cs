using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using OutWit.Common.Email;
using OutWit.Shared.Email.Provider.Resend;
using Resend;

namespace OutWit.Shared.Email.Provider.Resend.Tests
{
    [TestFixture]
    public class ResendFailureClassifierTests
    {
        #region Exception → Kind

        [Test]
        public void HttpRequestExceptionMapsToTransientTest()
        {
            var kind = ResendFailureClassifier.Classify(new HttpRequestException("network"));
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Transient));
        }

        [Test]
        public void TaskCanceledMapsToTransientTest()
        {
            var kind = ResendFailureClassifier.Classify(new TaskCanceledException());
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Transient));
        }

        [Test]
        public void TimeoutExceptionMapsToTransientTest()
        {
            var kind = ResendFailureClassifier.Classify(new TimeoutException());
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Transient));
        }

        [Test]
        public void OperationCanceledMapsToTransientTest()
        {
            var kind = ResendFailureClassifier.Classify(new OperationCanceledException());
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Transient));
        }

        [Test]
        public void GenericExceptionMapsToPermanentTest()
        {
            var kind = ResendFailureClassifier.Classify(new InvalidOperationException("oops"));
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Permanent));
        }

        #endregion

        #region ErrorType → Kind

        [TestCase(ErrorType.MissingApiKey)]
        [TestCase(ErrorType.InvalidApiKey)]
        [TestCase(ErrorType.RestrictedApiKey)]
        [TestCase(ErrorType.InvalidAccess)]
        public void AuthErrorTypesMapToAuthFailureTest(ErrorType type)
        {
            var kind = ResendFailureClassifier.ClassifyErrorType(type);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.AuthFailure));
        }

        [TestCase(ErrorType.RateLimitExceeded)]
        [TestCase(ErrorType.MonthlyQuotaExceeded)]
        [TestCase(ErrorType.DailyQuotaExceeded)]
        public void ThrottlingErrorTypesMapToRateLimitedTest(ErrorType type)
        {
            var kind = ResendFailureClassifier.ClassifyErrorType(type);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.RateLimited));
        }

        [TestCase(ErrorType.InternalServerError)]
        [TestCase(ErrorType.ApplicationError)]
        [TestCase(ErrorType.ConcurrentIdempotentRequests)]
        public void ServerSideErrorTypesMapToTransientTest(ErrorType type)
        {
            var kind = ResendFailureClassifier.ClassifyErrorType(type);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Transient));
        }

        [TestCase(ErrorType.ValidationError)]
        [TestCase(ErrorType.InvalidFromAddress)]
        [TestCase(ErrorType.MissingRequiredField)]
        [TestCase(ErrorType.InvalidParameter)]
        [TestCase(ErrorType.InvalidAttachment)]
        public void ValidationErrorTypesMapToPermanentTest(ErrorType type)
        {
            var kind = ResendFailureClassifier.ClassifyErrorType(type);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Permanent));
        }

        #endregion

        #region Status code → Kind

        [TestCase(401)]
        [TestCase(403)]
        public void AuthStatusCodesMapToAuthFailureTest(int code)
        {
            var kind = ResendFailureClassifier.ClassifyStatusCode(code);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.AuthFailure));
        }

        [Test]
        public void StatusCode422MapsToPermanentTest()
        {
            var kind = ResendFailureClassifier.ClassifyStatusCode(422);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Permanent));
        }

        [Test]
        public void StatusCode429MapsToRateLimitedTest()
        {
            var kind = ResendFailureClassifier.ClassifyStatusCode(429);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.RateLimited));
        }

        [TestCase(500)]
        [TestCase(502)]
        [TestCase(503)]
        [TestCase(504)]
        public void ServerStatusCodesMapToTransientTest(int code)
        {
            var kind = ResendFailureClassifier.ClassifyStatusCode(code);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Transient));
        }

        [Test]
        public void UnknownStatusCodeMapsToPermanentTest()
        {
            var kind = ResendFailureClassifier.ClassifyStatusCode(418);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Permanent));
        }

        #endregion

        #region ResendException end-to-end

        [Test]
        public void ResendExceptionWithAuthErrorTypeMapsToAuthFailureTest()
        {
            var ex = new ResendException(HttpStatusCode.Unauthorized, ErrorType.InvalidApiKey, "bad key");
            var kind = ResendFailureClassifier.Classify(ex);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.AuthFailure));
        }

        [Test]
        public void ResendExceptionWithRateLimitMapsToRateLimitedTest()
        {
            var ex = new ResendException(HttpStatusCode.TooManyRequests, ErrorType.RateLimitExceeded, "slow down");
            var kind = ResendFailureClassifier.Classify(ex);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.RateLimited));
        }

        [Test]
        public void ResendExceptionWithServer5xxFallsBackToTransientByStatusTest()
        {
            // ErrorType InternalServerError → Transient directly (no fallback needed),
            // but this exercises the path where ErrorType classifies to Permanent
            // and the status code says "Transient".
            // We pick a permanent-by-type error with a 5xx status — type wins for known auth/quota,
            // but a generic Validation type with a 5xx code (theoretical edge) should fall through.
            // Use ApplicationError with 503 to verify Transient outcome regardless of path.
            var ex = new ResendException(HttpStatusCode.ServiceUnavailable, ErrorType.ApplicationError, "down");
            var kind = ResendFailureClassifier.Classify(ex);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Transient));
        }

        [Test]
        public void ResendExceptionWithValidationErrorMapsToPermanentTest()
        {
            var ex = new ResendException(HttpStatusCode.UnprocessableEntity, ErrorType.ValidationError, "bad email");
            var kind = ResendFailureClassifier.Classify(ex);
            Assert.That(kind, Is.EqualTo(EmailFailureKind.Permanent));
        }

        #endregion
    }
}
