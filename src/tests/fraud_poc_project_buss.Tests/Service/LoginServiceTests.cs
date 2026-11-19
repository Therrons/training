using FluentAssertions;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Interfaces;
using fraud_poc_project_buss.Service;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace fraud_poc_project_buss.Tests.Service
{
    public class LoginServiceTests
    {
        private readonly Mock<IJwtService> _jwt = new();
        private readonly Mock<ILoginAttemptTracker> _tracker = new();
        private readonly Mock<IAuthenticationMetrics> _metrics = new();

        private LoginService CreateService() =>
            new(_jwt.Object, _tracker.Object, _metrics.Object, NullLogger<LoginService>.Instance);

        [Fact]
        public void Login_WithMissingPassword_ReturnsMissingCredentialsAndRecordsFailure()
        {
            var result = CreateService().Login("user", " ");

            result.Outcome.Should().Be(LoginOutcome.MissingCredentials);
            _metrics.Verify(m => m.RecordAuthenticationAttempt(false), Times.Once);
            _jwt.Verify(j => j.GenerateToken(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Login_WhenLockedOut_ReturnsLockedOutWithAtLeastSixtySecondsRetry()
        {
            _tracker.Setup(t => t.IsLockedOut("user", out It.Ref<DateTimeOffset>.IsAny))
                .Returns((string _, out DateTimeOffset until) => { until = DateTimeOffset.UtcNow.AddSeconds(10); return true; });

            var result = CreateService().Login("user", "pw");

            result.Outcome.Should().Be(LoginOutcome.LockedOut);
            result.RetryAfterSeconds.Should().Be(60, "the minimum retry wait is 60 seconds");
            _jwt.Verify(j => j.GenerateToken(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void Login_WithWrongCredentials_RecordsFailureAndReturnsInvalidCredentials()
        {
            _jwt.Setup(j => j.GenerateToken("user", "wrong")).Returns((ValueTuple<string, DateTime, DateTime>?)null);

            var result = CreateService().Login("user", "wrong");

            result.Outcome.Should().Be(LoginOutcome.InvalidCredentials);
            _tracker.Verify(t => t.RecordFailure("user"), Times.Once);
            _tracker.Verify(t => t.RecordSuccess(It.IsAny<string>()), Times.Never);
            _metrics.Verify(m => m.RecordAuthenticationAttempt(false), Times.Once);
        }

        [Fact]
        public void Login_WithValidCredentials_ReturnsTokenAndRecordsSuccess()
        {
            var validFrom = new DateTime(2026, 11, 18, 10, 0, 0);
            var validTo = validFrom.AddMinutes(60);
            _jwt.Setup(j => j.GenerateToken("user", "pw"))
                .Returns(("token-value", validFrom, validTo));
            _jwt.SetupGet(j => j.ExpirationMinutes).Returns(60);

            var result = CreateService().Login("user", "pw");

            result.Outcome.Should().Be(LoginOutcome.Succeeded);
            result.Token.Should().Be("token-value");
            result.ValidFrom.Should().Be(validFrom);
            result.ValidTo.Should().Be(validTo);
            result.ExpirationMinutes.Should().Be(60);
            _tracker.Verify(t => t.RecordSuccess("user"), Times.Once);
            _metrics.Verify(m => m.RecordAuthenticationAttempt(true), Times.Once);
        }
    }
}
