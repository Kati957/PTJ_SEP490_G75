using PTJ_Service.LocationService;
using System.Threading.Tasks;

public class FakeLocationDisplayService : LocationDisplayService
{
    public FakeLocationDisplayService() : base(null) { }

    public override Task<string> BuildAddressAsync(int provinceId, int districtId, int wardId)
    {
        return Task.FromResult("Fake Address");
    }
}
