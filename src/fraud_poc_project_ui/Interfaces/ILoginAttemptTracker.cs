using System;

namespace fraud_poc_project.Interfaces
{
    public interface ILoginAttemptTracker
    {
        bool IsLockedOut(string username, out DateTimeOffset lockedUntil);

        void RecordFailure(string username);

        void RecordSuccess(string username);
    }
}
