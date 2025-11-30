using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using uchet.Services;
using System.Threading.Tasks;

namespace uchet.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class TransferHistoryController : Controller
    {
        private readonly IPropertyTransferService _transferService;

        public TransferHistoryController(IPropertyTransferService transferService)
        {
            _transferService = transferService;
        }

        public async Task<IActionResult> Index()
        {
            var transfers = await _transferService.GetAllTransfersAsync();
            return View(transfers);
        }
    }
}