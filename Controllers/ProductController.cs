using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using opentelem.Data;
using opentelem.Models;

namespace opentelem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(ApplicationDbContext context) : ControllerBase
{
  private readonly ApplicationDbContext _context = context;

  [HttpGet("slow")]
  public async Task<IActionResult> GetSlowProducts()
  {
    var products = await _context.Products
        .GroupBy(l => l.Name)
        .Select(g => new { Name = g.Key, Count = g.Count() })
        .Where(g => g.Count > 1)
        .ToListAsync();
    return Ok(products);
  }

  [HttpGet]
  public async Task<IActionResult> GetProducts()
  {
    var products = await _context.Products.ToListAsync();
    return Ok(products);
  }

  [HttpPost]
  public async Task<IActionResult> CreateProduct(Product product)
  {
    _context.Products.Add(product);
    await _context.SaveChangesAsync();
    return CreatedAtAction(nameof(GetProducts), new { id = product.Id }, product);
  }
}
