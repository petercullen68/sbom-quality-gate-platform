using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeProductRepository(Product? productToReturn = null) : IProductRepository
{
    public bool AddCalled { get; private set; }
    public Product? AddedProduct { get; private set; }

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        AddCalled = true;
        AddedProduct = product;
        return Task.CompletedTask;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(productToReturn);
    }
}
