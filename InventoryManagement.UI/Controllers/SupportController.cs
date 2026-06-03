using System;
using System.Threading;
using System.Threading.Tasks;
using CourseProject_InventoryManagement.Application.DTOs;
using InventoryManagement.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.UI.Controllers
{
    public class SupportController : AppController
    {
        private readonly BackendApiClient _backendApiClient;

        public SupportController(BackendApiClient backendApiClient)
        {
            _backendApiClient = backendApiClient;
        }

        [HttpPost]
        public async Task<IActionResult> CreateTicket([FromBody] CreateSupportTicketRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Summary) || string.IsNullOrWhiteSpace(request.Link))
            {
                return Json(new { success = false, message = "Lütfen özet/açıklama alanını doldurun." });
            }

            // Forward the request to C# Web API General/CreateSupportTicket endpoint
            var apiResult = await _backendApiClient.PostAsync<CreateSupportTicketRequest, UploadedFileResultDto>(
                "General/CreateSupportTicket", 
                request, 
                requiresAuth: true, 
                cancellationToken);

            if (apiResult.IsSuccess && apiResult.Value != null)
            {
                return Json(new { 
                    success = true, 
                    message = $"Destek talebiniz başarıyla oluşturuldu ve buluta aktarıldı! Dosya adı: {apiResult.Value.FileName}",
                    url = apiResult.Value.Url 
                });
            }

            return Json(new { success = false, message = apiResult.ErrorMessage ?? "Destek talebi gönderilirken sunucu hatası oluştu." });
        }
    }

    public class CreateSupportTicketRequest
    {
        public string Summary { get; set; } = null!;
        public string Priority { get; set; } = "Average";
        public Guid? InventoryId { get; set; }
        public string Link { get; set; } = null!;
    }
}
