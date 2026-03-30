using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Infrastructure.Persistence;

public class ProductRepository(AppDbContext context) : IProductRepository
{
    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        context.Products.Add(product);
        return Task.CompletedTask;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Products
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
