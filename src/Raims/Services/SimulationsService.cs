namespace Raims
{
    /// <summary>Groups simulation operations that require a Sandbox API key.</summary>
    public sealed class SimulationsService
    {
        internal SimulationsService(ApiRequestor requestor)
        {
            Charges = new SimulationChargesService(requestor);
            Refunds = new SimulationRefundsService(requestor);
        }

        public SimulationChargesService Charges { get; }
        public SimulationRefundsService Refunds { get; }
    }
}
