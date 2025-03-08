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
    // this fails
    var products = await _context.Products
        .SelectMany(p1 => _context.Products, (p1, p2) => new { p1, p2 })
        .SelectMany(p => _context.Products, (p, p3) => new { p.p1, p.p2, p3 })
        .GroupBy(l => l.p1.Price, l => l.p2.Price)
        .Select(g => new { Price = g.Key, Count = g.Count() })
        .OrderByDescending(g => g.Count)
        .ThenByDescending(g => g.Price)
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
