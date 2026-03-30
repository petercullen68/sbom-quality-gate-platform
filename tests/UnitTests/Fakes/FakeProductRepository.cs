using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeProductRepository : IProductRepository
{
    private readonly Product? _productToReturn;

    public bool AddCalled { get; private set; }
    public Product? AddedProduct { get; private set; }

    public FakeProductRepository(Product? productToReturn = null)
    {
        _productToReturn = productToReturn;
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        AddCalled = true;
        AddedProduct = product;
        return Task.CompletedTask;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_productToReturn);
    }
}
