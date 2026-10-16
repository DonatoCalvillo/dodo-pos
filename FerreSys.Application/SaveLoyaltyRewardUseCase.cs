using FerreSys.Domain;

namespace FerreSys.Application;

public sealed class LoyaltyRewardDraft
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int PointsCost { get; init; }
    public LoyaltyRewardKind Kind { get; init; }
    public decimal Value { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class SaveLoyaltyRewardUseCase
{
    private readonly ILoyaltyRepository _loyalty;

    public SaveLoyaltyRewardUseCase(ILoyaltyRepository loyalty)
    {
        _loyalty = loyalty;
    }

    public LoyaltyReward Execute(LoyaltyRewardDraft draft)
    {
        string name = draft.Name.Trim();
        if (name.Length < 2 || name.Length > 80)
            throw new InvalidOperationException("El nombre del premio debe tener entre 2 y 80 caracteres.");

        string description = draft.Description.Trim();
        if (description.Length > 200)
            throw new InvalidOperationException("La descripción no puede pasar de 200 caracteres.");

        if (draft.Id is Guid id && _loyalty.GetReward(id) == null)
            throw new InvalidOperationException("El premio ya no existe.");

        var reward = new LoyaltyReward(
            draft.Id ?? Guid.NewGuid(),
            name,
            description,
            draft.PointsCost,
            draft.Kind,
            draft.Value,
            draft.IsActive);

        _loyalty.SaveReward(reward);
        return reward;
    }
}
