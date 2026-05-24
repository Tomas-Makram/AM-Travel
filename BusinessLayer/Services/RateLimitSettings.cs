namespace BusinessLayer.Services
{
    public sealed class RateLimitSettings
    {
        public string RejectionMessage { get; set; } = "Too many requests. Please try again later.";

        public SlidingWindowPolicySettings GlobalLimiter { get; set; } = new();
        public SlidingWindowPolicySettings ApiDefaultPolicy { get; set; } = new();
        public SlidingWindowPolicySettings AuthenticatedUserPolicy { get; set; } = new();
        public ConcurrencyPolicySettings PerUserHeavyOpsPolicy { get; set; } = new();
        public SlidingWindowPolicySettings LoginPolicy { get; set; } = new();
        public FixedWindowPolicySettings RegisterPolicy { get; set; } = new();
        public SlidingWindowPolicySettings RefreshPolicy { get; set; } = new();
        public SlidingWindowPolicySettings LogoutPolicy { get; set; } = new();
        public ConcurrencyPolicySettings HeavyOpsPolicy { get; set; } = new();
        public FixedWindowPolicySettings BurstPerPathPolicy { get; set; } = new();
    }

    public sealed class SlidingWindowPolicySettings
    {
        public int PermitLimit { get; set; } = 1;
        public int WindowMinutes { get; set; } = 1;
        public int SegmentsPerWindow { get; set; } = 1;
        public int QueueLimit { get; set; } = 0;
    }

    public sealed class FixedWindowPolicySettings
    {
        public int PermitLimit { get; set; } = 1;
        public int WindowMinutes { get; set; } = 1;
        public int QueueLimit { get; set; } = 0;
    }

    public sealed class ConcurrencyPolicySettings
    {
        public int PermitLimit { get; set; } = 1;
        public int QueueLimit { get; set; } = 0;
    }
}