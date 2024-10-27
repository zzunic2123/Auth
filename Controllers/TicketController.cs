using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using Auth.Data;
using Auth.Data.Entities;
using Auth.Models;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json.Linq;
using QRCoder;


namespace Auth.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]/[action]")]
[ApiController]
public class TicketController : ControllerBase
{
    private readonly DataContext _context;
    private readonly HttpClient _httpClient;


    public TicketController(DataContext context, HttpClient httpClient)
    {
        _context = context;
        _httpClient = httpClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuthToken()
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "https://dev-y63kymsgnlsf7ana.us.auth0.com/oauth/token");
        request.Content = new StringContent(
            "{\"client_id\":\"onuZkFhaszprEZ1YZrJv1YEcdwnwutWV\",\"client_secret\":\"rAqEFyKr6wkDGn5223hLsFdd_sNU6roP4rKKObPpFIGg0StstUvxbP18CmQQXpCD\",\"audience\":\"https://dev-y63kymsgnlsf7ana.us.auth0.com/api/v2/\",\"grant_type\":\"client_credentials\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        return Content(content, "application/json");
    }
    
    [HttpGet]
    public async Task<IActionResult> GetTicketCount()
    {
        var count = await _context.Tickets.CountAsync();
        return Ok(new { ticketCount = count });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> GenerateTicket([FromBody] TicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VATIN) || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return BadRequest("Missing required fields: VATIN, FirstName, LastName.");
        }

        var existingTickets = await _context.Tickets.CountAsync(t => t.VATIN == request.VATIN);
        if (existingTickets >= 3)
        {
            return BadRequest("A maximum of 3 tickets can be generated for this VATIN.");
        }

        var ticket = new Ticket
        {
            VATIN = request.VATIN,
            FirstName = request.FirstName,
            LastName = request.LastName
        };
        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        string ticketUrl = $"https://be-fer-nrppw-windows-ew-g6h2ghb6gkajhxcj.westeurope-01.azurewebsites.net/tickets/{ticket.Id}";


        try
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(ticketUrl, QRCodeGenerator.ECCLevel.Q))
            using (QRCode qrCode = new QRCode(qrCodeData))
            using (Bitmap qrCodeImage = qrCode.GetGraphic(20))
            using (MemoryStream ms = new MemoryStream())
            {
                qrCodeImage.Save(ms, ImageFormat.Png);
                return File(ms.ToArray(), "image/png");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Error generating QR code: " + ex.Message);
        }
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicketDetails(Guid id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound("Ticket not found.");
        }

        return Ok(new
        {
            ticket.VATIN,
            ticket.FirstName,
            ticket.LastName,
            ticket.CreatedAt
        });
    }
    
}
