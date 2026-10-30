using System;

namespace Raims
{
    /// <summary>An isolated client for the published Raims API.</summary>
    public sealed class RaimsClient : IDisposable
    {
        public const string Version = "0.1.5";
        public const string DefaultBaseUrl = "https://api.raims.com";
        private readonly ApiRequestor requestor;

        public RaimsClient(string apiKey) : this(new RaimsClientOptions { ApiKey = apiKey }) { }

        public RaimsClient(RaimsClientOptions options)
        {
            requestor = new ApiRequestor(options);
            Accounts = new AccountsService(requestor);
            Balances = new BalancesService(requestor);
            Charges = new ChargesService(requestor);
            Customers = new CustomersService(requestor);
            Disputes = new DisputesService(requestor);
            PaymentLinks = new PaymentLinksService(requestor);
            Refunds = new RefundsService(requestor);
            Statements = new StatementsService(requestor);
            Webhooks = new WebhooksService(requestor);
            WebhookEventTypes = new WebhookEventTypesService(requestor);
            Withdrawals = new WithdrawalsService(requestor);
            WithdrawalDestinations = new WithdrawalDestinationsService(requestor);
            Simulations = new SimulationsService(requestor);
        }

        public AccountsService Accounts { get; }
        public BalancesService Balances { get; }
        public ChargesService Charges { get; }
        public CustomersService Customers { get; }
        public DisputesService Disputes { get; }
        public PaymentLinksService PaymentLinks { get; }
        public RefundsService Refunds { get; }
        public StatementsService Statements { get; }
        public WebhooksService Webhooks { get; }
        public WebhookEventTypesService WebhookEventTypes { get; }
        public WithdrawalsService Withdrawals { get; }
        public WithdrawalDestinationsService WithdrawalDestinations { get; }
        public SimulationsService Simulations { get; }

        /// <summary>Releases the default transport. A caller-supplied transport remains caller-owned.</summary>
        public void Dispose() { requestor.Dispose(); }

        public override string ToString() { return "RaimsClient/" + Version; }
    }
}
