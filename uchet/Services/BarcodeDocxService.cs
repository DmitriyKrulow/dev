using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using uchet.Models;
using Microsoft.AspNetCore.Hosting;

namespace uchet.Services
{
    public class BarcodeDocxService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpClientFactory _httpClientFactory;

        public BarcodeDocxService(IHttpClientFactory httpClientFactory, IWebHostEnvironment environment)
        {
            _httpClientFactory = httpClientFactory;
            _environment = environment;
        }
        
        public byte[] GenerateBarcodeDocument(IEnumerable<Property> properties, string baseUrl)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var document = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
                {
                    // Создаем основную часть документа
                    document.AddMainDocumentPart();
                    document.MainDocumentPart.Document = new Document();
                    document.MainDocumentPart.Document.Body = new Body();
                    
                    // Создаем таблицу для бирок
                    var table = new Table();
                    
                    // Определяем свойства таблицы
                    var tableProperties = new TableProperties(
                        new TableBorders(
                            new TopBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                            new BottomBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                            new LeftBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                            new RightBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                            new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 },
                            new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 12 }
                        )
                    );
                    
                    table.AppendChild(tableProperties);
                    
                    // Создаем строки таблицы
                    int rowIndex = 0;
                    TableRow currentRow = null;
                    
                    foreach (var property in properties)
                    {
                        // Создаем новую строку каждые 3 бирки
                        if (rowIndex % 3 == 0)
                        {
                            currentRow = new TableRow();
                            table.AppendChild(currentRow);
                        }
                        
                        // Создаем ячейку для бирки
                        var cell = new TableCell();
                        
                        // Создаем содержимое ячейки
                        var cellContent = new Paragraph(new Run(new Text($"{property.Name}")));
                        cell.AppendChild(cellContent);
                        
                        // Добавляем инвентарный номер
                        var inventoryParagraph = new Paragraph(new Run(new Text($"Инв. №: {property.InventoryNumber}")));
                        cell.AppendChild(inventoryParagraph);
                        
                        // Добавляем изображение штрих-кода
                        if (!string.IsNullOrEmpty(property.Barcode))
                        {
                            // Создаем URL для получения изображения штрих-кода
                            var barcodeUrl = $"{baseUrl}/Property/GenerateBarcodeImage/{property.Id}";
                            
                            // Добавляем текстовый placeholder для штрих-кода
                            var barcodeParagraph = new Paragraph(new Run(new Text($"Штрих-код: {property.Barcode}")));
                            cell.AppendChild(barcodeParagraph);
                        }
                        
                        // Добавляем изображение QR-кода
                        if (!string.IsNullOrEmpty(property.QRCode))
                        {
                            // Создаем URL для получения изображения QR-кода
                            var qrCodeUrl = $"{baseUrl}/Property/GenerateQRCodeImage/{property.Id}";
                            
                            // Добавляем текстовый placeholder для QR-кода
                            var qrCodeParagraph = new Paragraph(new Run(new Text($"QR-код: {property.QRCode}")));
                            cell.AppendChild(qrCodeParagraph);
                        }
                        
                        // Устанавливаем свойства ячейки
                        cell.TableCellProperties = new TableCellProperties(
                            new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = "3000" } // Примерная ширина
                        );
                        
                        currentRow.AppendChild(cell);
                        rowIndex++;
                    }
                    
                    // Добавляем таблицу в документ
                    document.MainDocumentPart.Document.Body.AppendChild(table);
                    
                    // Сохраняем документ
                    document.Save();
                }
                
                return memoryStream.ToArray();
            }
        }
    }
}