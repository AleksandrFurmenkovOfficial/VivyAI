using VivyAI.Interfaces;

namespace VivyAI.Implementation
{
    internal sealed class AdminChecker : IAdminChecker
    {
        private readonly string adminUserId;

        public AdminChecker(string adminUserId)
        {
            this.adminUserId = adminUserId;
        }

        public bool IsAdmin(string userId)
        {
            return string.Equals(userId, adminUserId, StringComparison.OrdinalIgnoreCase);
        }
    }
}