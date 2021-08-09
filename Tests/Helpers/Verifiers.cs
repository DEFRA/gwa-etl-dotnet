using System;
using Defra.Gwa.Etl;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExtractAWDataTests
{
    public class Verifiers
    {
        public static void VerifyLog(Mock<ILogger<ExtractAWData>> loggerMock, LogLevel logLevel, string message)
        {
            loggerMock.Verify(logger => logger.Log(
                It.Is<LogLevel>(level => level == logLevel),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()
            ));
        }

        public static void VerifyLogError(Mock<ILogger<ExtractAWData>> loggerMock, string message)
        {
            VerifyLog(loggerMock, LogLevel.Error, message);
        }

        public static void VerifyLogInfo(Mock<ILogger<ExtractAWData>> loggerMock, string message)
        {
            VerifyLog(loggerMock, LogLevel.Information, message);
        }

        public static void VerifyLogInfoReport(Mock<ILogger<ExtractAWData>> loggerMock, ReportLog reportLog)
        {
            VerifyLogInfo(loggerMock, $"Data extract from AW is complete.\n{reportLog.DevicesProcessed} devices have been processed.");
            VerifyLogInfo(loggerMock, $"{reportLog.DevicesWithUserEmailAddress} devices have a UserEmailAddress of which {reportLog.DevicesWithNoPhoneNumber} have no PhoneNumber.");
            VerifyLogInfo(loggerMock, $"{reportLog.DevicesWithNoUserEmailAddress} devices with no UserEmailAddress.");
            VerifyLogInfo(loggerMock, $"{reportLog.IPads} iPads have been ignored.");
        }
    }
}
