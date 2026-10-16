namespace FerreSys.Domain;

public interface ILoyaltyRepository
{
    IReadOnlyList<CustomerBalance> ListCustomers();

    Customer? GetCustomer(Guid id);

    Customer? FindByPhone(string phone);

    IReadOnlyList<CustomerBalance> SearchActive(string term, int limit = 8);

    int GetBalance(Guid customerId);

    void SaveCustomer(Customer customer);

    IReadOnlyList<LoyaltyReward> ListRewards();

    LoyaltyReward? GetReward(Guid id);

    void SaveReward(LoyaltyReward reward);

    LoyaltyReversalTotals GetReversalTotals(Guid saleId);
}
