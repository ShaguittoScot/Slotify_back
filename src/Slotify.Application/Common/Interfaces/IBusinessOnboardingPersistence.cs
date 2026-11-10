using Slotify.Domain.Entities;

namespace Slotify.Application.Common.Interfaces;

public interface IBusinessOnboardingPersistence
{
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default
    );

    void AddBusiness(Business business);

    void AddUser(User user);

    void AddBusinessBrand(BusinessBrand brand);

    void AddServices(IEnumerable<Service> services);

    void AddBusinessHours(IEnumerable<AvailabilitySchedule> schedules);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<User?> GetUserWithBusinessDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<bool> HasBusinessHoursAsync(
        Guid businessId,
        CancellationToken cancellationToken = default
    );
}
